using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.ScholarshipOffer;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
    public class ScholarshipService : IScholarshipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ScholarshipService> _logger;

        public ScholarshipService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<ScholarshipService> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }
        public async Task<ScholarshipOfferDto> CreateOfferAsync(CreateScholarshipOfferDto dto, string userId, bool isDirector)
        {
            int currentFiscalYear = DateTime.UtcNow.Year;
            var budget = await _unitOfWork.BandBudgets.GetQueryable()
                .FirstOrDefaultAsync(b => b.BandId == dto.BandId && b.FiscalYear == currentFiscalYear);

            if (budget == null)
                throw new InvalidOperationException($"No budget defined for fiscal year {currentFiscalYear}.");

            // 2. Validate Budget
            if (budget.RemainingAmount < dto.Amount)
            {
                throw new InvalidOperationException($"Insufficient budget. Remaining: ${budget.RemainingAmount:N0}");
            }

            budget.AllocatedAmount += dto.Amount;
            budget.RemainingAmount -= dto.Amount;

            _unitOfWork.BandBudgets.Update(budget);
            // 1. Fetch the Band to check/deduct budget
            // Note: In a real scenario, use dto.BandId. Using 1 here to match previous hardcoding logic.
            int bandId = dto.BandId;
            var band = await _unitOfWork.Bands.GetByIdAsync(bandId);

            if (band == null)
                throw new KeyNotFoundException("Band not found.");

            //// 2. Validate Budget
            //if (band.ScholarshipBudget < dto.ScholarshipAmount)
            //{
            //    throw new InvalidOperationException($"Insufficient scholarship budget. Remaining: ${band.ScholarshipBudget}");
            //}

            // 3. Deduct from Budget (Optimistic Concurrency will protect this) 
            //band.ScholarshipBudget -= dto.ScholarshipAmount;
            _unitOfWork.BandBudgets.Update(budget);

            var offer = new ScholarshipOffer
            {
                StudentId = dto.StudentId,
                BandId = band.Id,
                CreatedByUserId = userId,
                ScholarshipAmount = dto.Amount,
                Description = dto.Description,
                OfferType = dto.OfferType,
                // LOGIC: Directors skip approval, Recruiters go to Pending
                RequiresGuardianApproval = dto.RequiresGuardianApproval,
                Status = isDirector ? ScholarshipStatus.Sent : ScholarshipStatus.PendingApproval,
                ExpirationDate = DateTime.UtcNow.AddDays(30) // Default 30 days
            };

            await _unitOfWork.ScholarshipOffers.AddAsync(offer);

            try
            {
                var audit = new AuditLog
                {
                    ApplicationUserId = userId,
                    ActionType = "CreateScholarshipOffer",
                    Description = $"Created {dto.OfferType} offer of ${dto.Amount} for Student #{dto.StudentId}",
                    CreatedAt = DateTime.UtcNow,
                    MetadataJson = JsonSerializer.Serialize(new { dto.StudentId, dto.BandId, dto.Amount })
                };
                await _unitOfWork.AuditLogs.AddAsync(audit);
            }
            catch (Exception)
            {
                throw new Exception("Could not log this scholarship");
            }


            try
            {
                // 4. Attempt to Save (Updates Band Budget AND Inserts Offer in one transaction)
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // 5. Handle Concurrency Conflict
                // This occurs if another user modified the Band record (e.g. another deduction) 
                // between the time we fetched 'band' and now.
                throw new InvalidOperationException("The budget was updated by another transaction. Please try again.");
            }

            // Trigger Notification (SignalR) if offer is Sent
            if (offer.Status == ScholarshipStatus.Sent)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(dto.StudentId);
                if (student != null)
                {
                    await _notificationService.NotifyUserAsync(
                        student.ApplicationUserId,
                        "ScholarshipOffer",
                        "New Scholarship Offer",
                        $"You have received a scholarship offer from {band.BandName}.",
                        offer.Id.ToString());
                }
            }

            return MapToDto(offer);
        }

        public async Task<ScholarshipOfferDto> GetOfferByIdAsync(int id)
        {
            // 1. Fetch with includes to ensure related data (Names) are available for the DTO
            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Include(o => o.Band)
                .Include(o => o.CreatedByStaff)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null) return null;

            // 2. Use the existing helper to return a safe DTO
            return MapToDto(offer);
        }

        public async Task ApproveOfferAsync(int offerId, string directorId)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) throw new KeyNotFoundException("Offer not found");

            if (offer.Status != ScholarshipStatus.PendingApproval)
                throw new InvalidOperationException($"Cannot approve offer in status {offer.Status}");

            offer.Status = ScholarshipStatus.Sent;
            offer.ApprovedByUserId = directorId;
            offer.ApprovedAt = DateTime.UtcNow;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();

            // Notify Student
            var student = await _unitOfWork.Students.GetByIdAsync(offer.StudentId);
            if (student != null)
            {
                await _notificationService.NotifyUserAsync(
                    student.ApplicationUserId,
                    "ScholarshipApproved",
                    "Scholarship Approved",
                    "Your scholarship offer has been approved.",
                    offer.Id.ToString());
            }
        }

        public async Task RespondToOfferAsync(int offerId, RespondToScholarshipOfferDto dto, string userId, bool isGuardian)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) throw new KeyNotFoundException("Offer not found");

            // VALIDATION RULES
            if (offer.Status != ScholarshipStatus.Sent)
                throw new InvalidOperationException("This offer is not currently open for response.");

            if (DateTime.UtcNow > offer.ExpirationDate)
            {
                offer.Status = ScholarshipStatus.Expired;
                await _unitOfWork.SaveChangesAsync(); // Save the status change to Expired
                throw new InvalidOperationException("This offer has expired.");
            }

            // [NEW] Audit Log for Response
            try
            {
                var audit = new AuditLog
                {
                    ApplicationUserId = userId,
                    ActionType = isGuardian ? "GuardianRespondOffer" : "StudentRespondOffer",
                    Description = $"{(isGuardian ? "Guardian" : "Student")} {(dto.Accept ? "Accepted" : "Declined")} Offer #{offerId}",
                    CreatedAt = DateTime.UtcNow,
                    MetadataJson = JsonSerializer.Serialize(new { OfferId = offerId, Accepted = dto.Accept, Comment = dto.Notes })
                };
                await _unitOfWork.AuditLogs.AddAsync(audit);
            }
            catch (Exception)
            {

                throw new Exception("Could not log this scholarship Acceptance or Rejection");
            }

            if (dto.Accept)
            {
                // If responder is a Student (not guardian) and approval is required
                if (!isGuardian && offer.RequiresGuardianApproval)
                {
                    offer.Status = ScholarshipStatus.PendingGuardianSignature;
                }
                else
                {
                    offer.Status = ScholarshipStatus.Accepted;
                }
            }
            else
            {
                offer.Status = ScholarshipStatus.Declined;

                // Since the offer is declined, we must return the funds to the budget 
                // from which they were originally allocated.
                int fiscalYear = offer.CreatedAt.Year;

                var budget = await _unitOfWork.BandBudgets.GetQueryable()
                    .FirstOrDefaultAsync(b => b.BandId == offer.BandId && b.FiscalYear == fiscalYear);

                // It's possible (though rare) that the budget record was deleted or fiscal year changed logic, 
                // so we null check.
                if (budget != null)
                {
                    budget.AllocatedAmount -= offer.ScholarshipAmount;
                    budget.RemainingAmount += offer.ScholarshipAmount;
                    _unitOfWork.BandBudgets.Update(budget);
                }
            }

            offer.ResponseNotes = dto.Notes;
            offer.ResponseDate = DateTime.UtcNow;


            if (isGuardian)
            {
                offer.RespondedByGuardianUserId = userId;
                offer.RespondedByGuardian = true;
            }
            else
            {
                offer.RespondedByUserId = userId;
            }

            offer.ResponseNotes = dto.Notes;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();
            // Notify Band Staff/Director of response (Accepted or Declined)
            if (offer.Status == ScholarshipStatus.Accepted || offer.Status == ScholarshipStatus.Declined)
            {
                var student = await _unitOfWork.Students.GetByIdAsync(offer.StudentId);
                string studentName = student != null ? $"{student.FirstName} {student.LastName}" : "A student";

                await _notificationService.NotifyBandStaffAsync(
                    offer.BandId,
                    "ScholarshipResponse",
                    "Scholarship Response",
                    $"{studentName} has {offer.Status.ToString().ToLower()} the scholarship offer.",
                    offer.Id.ToString());
            }

            // [NEW] Notify Guardians (Activity Tracking)
            // Query StudentGuardians to find linked users
            var linkedGuardians = await _unitOfWork.StudentGuardians.GetQueryable()
                .Include(sg => sg.Guardian)
                .ThenInclude(g => g.ApplicationUser)
                .Where(sg => sg.StudentId == offer.StudentId)
                .Select(sg => sg.Guardian.ApplicationUserId)
                .ToListAsync();

            foreach (var guardianUserId in linkedGuardians)
            {
                // Don't notify the user who just performed the action (if it was a guardian)
                if (guardianUserId == userId) continue;

                string action = dto.Accept ? "accepted" : "declined";
                await _notificationService.NotifyUserAsync(
                    guardianUserId,
                    "ScholarshipActivity",
                    "Scholarship Update",
                    $"The scholarship offer from {offer.Band?.BandName ?? "Band"} was {action}.",
                    offer.Id.ToString()
                );
            }

            // Note: Budget committed/available amounts are computed dynamically in GetBudgetStatsAsync
            // by querying offer statuses. AllocatedAmount was decremented on decline above; no further
            // budget update needed here for the accept path (funds stay allocated until accepted/expired/rescinded).
        }

        public async Task GuardianFinalizeOfferAsync(int offerId, string guardianUserId, bool accept)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) throw new KeyNotFoundException("Offer not found");

            // Can only finalize if it is pending signature
            if (offer.Status != ScholarshipStatus.PendingGuardianSignature)
                throw new InvalidOperationException($"Cannot finalize offer in status {offer.Status}. Expected PendingGuardianSignature.");

            // Check expiration
            if (DateTime.UtcNow > offer.ExpirationDate)
            {
                offer.Status = ScholarshipStatus.Expired;
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOperationException("This offer has expired.");
            }

            offer.Status = accept ? ScholarshipStatus.Accepted : ScholarshipStatus.Declined;

            offer.RespondedByGuardianUserId = guardianUserId;
            offer.RespondedByGuardian = true;
            offer.ResponseDate = DateTime.UtcNow; 

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RescindOfferAsync(int offerId, RescindScholarshipRequest dto, string directorId)
        {
            //Get Offer
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) throw new KeyNotFoundException();

            // Validate Status
            if (offer.Status == ScholarshipStatus.Accepted || offer.Status == ScholarshipStatus.Declined || offer.Status == ScholarshipStatus.Rescinded)
                throw new InvalidOperationException($"Cannot rescind offer in status {offer.Status}.");

            // 3. Refund Budget (Since we deducted it upon Creation)
            int fiscalYear = offer.CreatedAt.Year;
            var budget = await _unitOfWork.BandBudgets.GetQueryable()
                .FirstOrDefaultAsync(b => b.BandId == offer.BandId && b.FiscalYear == fiscalYear);

            if (budget != null)
            {
                budget.AllocatedAmount -= offer.ScholarshipAmount;
                budget.RemainingAmount += offer.ScholarshipAmount;
                _unitOfWork.BandBudgets.Update(budget);
            }

            // 4. Update Offer
            var previousStatus = offer.Status;
            offer.Status = ScholarshipStatus.Rescinded;
            offer.RescindedByUserId = directorId;
            offer.RescindReason = dto.Reason;
            offer.RescindedDate = DateTime.UtcNow;
            _unitOfWork.ScholarshipOffers.Update(offer); ;

            // 5. Create Permanent Audit Record
            var auditRecord = new AuditLog
            {
                ApplicationUserId = directorId,
                ActionType = "RescindOffer",
                Description = $"Director rescinded offer #{offerId}",
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    OfferId = offer.Id,
                    DirectorId = directorId,
                    Reason = dto.Reason,
                    AmountReturned = offer.ScholarshipAmount,
                    PreviousStatus = fiscalYear
                })
            };

            await _unitOfWork.AuditLogs.AddAsync(auditRecord);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ScholarshipBudgetDto> GetBudgetStatsAsync(int bandId)
        {
            int currentFiscalYear = DateTime.UtcNow.Year;

            // 1. Fetch Budget Data from BandBudget Table
            var budget = await _unitOfWork.BandBudgets.GetQueryable()
                .FirstOrDefaultAsync(b => b.BandId == bandId && b.FiscalYear == currentFiscalYear);

            if (budget == null)
            {
                // Return zeros if no budget configured yet
                return new ScholarshipBudgetDto
                {
                    TotalBudget = 0,
                    AvailableAmount = 0,
                    AllocatedAmount = 0,
                    PendingAmount = 0
                };
            }

            

            // 2. Calculate Pending vs Committed based on Offer Status
            // Note: AllocatedAmount in BandBudget includes BOTH Pending and Accepted offers.
            // We query offers to split the "Allocated" visualization for the UI.
            var offerStats = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId && o.CreatedAt.Year == currentFiscalYear)
                .GroupBy(o => 1)
                .Select(g => new
                {
                    Committed = g.Where(o => o.Status == ScholarshipStatus.Accepted).Sum(o => (decimal?)o.ScholarshipAmount) ?? 0,
                    Pending = g.Where(o => o.Status == ScholarshipStatus.Sent || o.Status == ScholarshipStatus.PendingApproval || o.Status == ScholarshipStatus.PendingGuardianSignature).Sum(o => (decimal?)o.ScholarshipAmount) ?? 0
                })
                .FirstOrDefaultAsync();

            return new ScholarshipBudgetDto
            {
                TotalBudget = budget.TotalBudget,
                AvailableAmount = budget.RemainingAmount,
                // Mapped from calculated aggregation for detailed breakdown
                CommittedAmount = offerStats?.Committed ?? 0,
                PendingAmount = offerStats?.Pending ?? 0
                // Note: Committed + Pending should roughly equal budget.AllocatedAmount
            };
        }

        // Helper mapper
        private static ScholarshipOfferDto MapToDto(ScholarshipOffer offer) => new ScholarshipOfferDto
        {
            OfferId = offer.Id,
            StudentId = offer.StudentId,
            StudentName = offer.Student != null ? $"{offer.Student.FirstName} {offer.Student.LastName}" : "",
            BandId = offer.BandId,
            BandName = offer.Band != null ? offer.Band.BandName : "",
            Status = offer.Status,
            Amount = offer.ScholarshipAmount,
            OfferType = offer.OfferType,
            CreatedAt = offer.CreatedAt,
            ApprovedAt = offer.ApprovedAt,
            ResponseDate = offer.ResponseDate,
            ExpirationDate = offer.ExpirationDate,
            Notes = offer.Description,
            Terms = offer.Terms,
            RescindReason = offer.RescindReason,
            CreatedByStaffName = offer.CreatedByStaff != null ? offer.CreatedByStaff.ApplicationUserId : "Unknown",
            ApprovedByUserId = offer.ApprovedByUserId,
            RespondedByGuardianUserId = offer.RespondedByGuardianUserId,
            RequiresGuardianApproval = offer.RequiresGuardianApproval
        };

        public async Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters)
        {
            // 1. Get the Director's Active Band
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);

            if (band == null)
                throw new KeyNotFoundException("Active band not found for this director.");

            // 2. Start the Query
            var query = _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(so => so.BandId == band.Id)
                .AsQueryable();

            // 3. Apply Filters
            if (!string.IsNullOrEmpty(filters.Status))
            {
                if (Enum.TryParse<ScholarshipStatus>(filters.Status, true, out var statusEnum))
                {
                    query = query.Where(so => so.Status == statusEnum);
                }
            }

            if (filters.MinAmount.HasValue)
                query = query.Where(so => so.ScholarshipAmount >= filters.MinAmount.Value);

            if (filters.MaxAmount.HasValue)
                query = query.Where(so => so.ScholarshipAmount <= filters.MaxAmount.Value);

            if (filters.CreatedAfter.HasValue)
                query = query.Where(so => so.CreatedAt >= filters.CreatedAfter.Value);

            if (filters.CreatedBefore.HasValue)
                query = query.Where(so => so.CreatedAt <= filters.CreatedBefore.Value);

            // 4. Calculate Statistics
            var summary = await query
                .GroupBy(so => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    TotalAmount = g.Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    PendingCount = g.Count(so => so.Status == ScholarshipStatus.Pending),
                    ApprovedCount = g.Count(so => so.Status == ScholarshipStatus.Approved),
                    AcceptedCount = g.Count(so => so.Status == ScholarshipStatus.Accepted),
                    DeclinedCount = g.Count(so => so.Status == ScholarshipStatus.Declined),
                    RescindedCount = g.Count(so => so.Status == ScholarshipStatus.Rescinded)
                })
                .FirstOrDefaultAsync();

            // 5. Get Paginated List
            var offers = await query
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .OrderByDescending(so => so.CreatedAt)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(so => MapToDto(so))
                .ToListAsync();

            // 6. Return Overview
            int currentFiscalYear = DateTime.UtcNow.Year;
            var budget = await _unitOfWork.BandBudgets.GetQueryable()
                .FirstOrDefaultAsync(b => b.BandId == band.Id && b.FiscalYear == currentFiscalYear);

            return new ScholarshipOverviewDto
            {
                TotalOffers = summary?.TotalCount ?? 0,
                TotalAmount = summary?.TotalAmount ?? 0m,
                PendingCount = summary?.PendingCount ?? 0,
                ApprovedCount = summary?.ApprovedCount ?? 0,
                AcceptedCount = summary?.AcceptedCount ?? 0,
                DeclinedCount = summary?.DeclinedCount ?? 0,
                AvailableBudget = budget?.RemainingAmount ?? 0m,
                Offers = offers,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalPages = (int)Math.Ceiling((double)(summary?.TotalCount ?? 0) / filters.PageSize)
            };
        }

        public async Task<ServiceResult<PagedResult<OfferSummaryDto>>> GetStudentOfferSummariesAsync(int studentId, int page, int pageSize)
        {
            var result = await _unitOfWork.ScholarshipOffers.GetPagedProjectionAsync(
                predicate: o => o.StudentId == studentId && !o.IsDeleted,
                selector: o => new OfferSummaryDto
                {
                    Id = o.Id,
                    BandName = o.Band != null ? o.Band.BandName : "Unknown Band",
                    UniversityName = o.Band != null ? o.Band.UniversityName : null,
                    Location = o.Band != null ? o.Band.City + ", " + o.Band.State : "",
                    Amount = o.ScholarshipAmount,
                    Status = o.Status.ToString(),
                    OfferType = o.OfferType,
                    ExpirationDate = o.ExpirationDate
                },
                page: page,
                pageSize: pageSize,
                orderBy: o => o.ExpirationDate
            );

            return ServiceResult<PagedResult<OfferSummaryDto>>.Success(result);
        }

        public async Task CheckExpirationsAsync()
        {
            // 2. Query for expired offers that are still pending/sent
            var expiredOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => (o.Status == ScholarshipStatus.Sent || o.Status == ScholarshipStatus.PendingGuardianSignature)
                            && o.ExpirationDate < DateTime.UtcNow)
                .ToListAsync();

            if (!expiredOffers.Any()) return;

            // 3. Process Budget Refunds (Optimization: Group by Budget to minimize DB calls)
            // When an offer expires, the money allocated to it should be returned to the budget.
            var refundsNeeded = expiredOffers
                .GroupBy(o => new { o.BandId, Year = o.CreatedAt.Year })
                .Select(g => new
                {
                    BandId = g.Key.BandId,
                    FiscalYear = g.Key.Year,
                    TotalRefund = g.Sum(o => o.ScholarshipAmount)
                })
                .ToList();

            foreach (var refund in refundsNeeded)
            {
                var budget = await _unitOfWork.BandBudgets.GetQueryable()
                    .FirstOrDefaultAsync(b => b.BandId == refund.BandId && b.FiscalYear == refund.FiscalYear);

                if (budget != null)
                {
                    budget.AllocatedAmount -= refund.TotalRefund;
                    budget.RemainingAmount += refund.TotalRefund;
                    _unitOfWork.BandBudgets.Update(budget);
                }
            }

            // 4. Update Offer Status
            foreach (var offer in expiredOffers)
            {
                offer.Status = ScholarshipStatus.Expired;
                // Optional: You could set a 'RespondedDate' or similar timestamp if you track expiration time specifically
                _unitOfWork.ScholarshipOffers.Update(offer);
            }

            // 5. Save Changes
            await _unitOfWork.SaveChangesAsync();

            // 6. Log the result
            _logger.LogInformation("Expired {Count} scholarship offers and refunded associated budgets.", expiredOffers.Count);
        }


    }
}
