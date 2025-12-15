using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Offer;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
    public class ScholarshipService : IScholarshipService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScholarshipService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ScholarshipOfferDto> CreateOfferAsync(CreateOfferDto dto, string userId, bool isDirector)
        {
            // 1. Validate Budget (Optional check before creating)

            var offer = new ScholarshipOffer
            {
                StudentId = dto.StudentId,
                BandId = 1, // Retrieve from current user context in real app
                CreatedByUserId = userId,
                ScholarshipAmount = dto.ScholarshipAmount,
                Description = dto.Description,
                OfferType = dto.OfferType,
                // LOGIC: Directors skip approval, Recruiters go to Pending
                Status = isDirector ? ScholarshipStatus.Sent : ScholarshipStatus.PendingApproval,
                ExpirationDate = DateTime.UtcNow.AddDays(30) // Default 30 days
            };

            await _unitOfWork.ScholarshipOffers.AddAsync(offer);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Trigger Notification (SignalR)
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
            // TODO: Notify Student
        }

        public async Task RespondToOfferAsync(int offerId, RespondToOfferDto dto, string userId, bool isGuardian)
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

            offer.Status = dto.Accept ? ScholarshipStatus.Accepted : ScholarshipStatus.Declined;
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

            offer.ResponseNotes = dto.Comment;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();
            // TODO: Update Budget Committed Amount
        }

        public async Task RescindOfferAsync(int offerId, RescindScholarshipRequest dto, string directorId)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) throw new KeyNotFoundException();

            // Can only rescind if it hasn't been finalized yet
            if (offer.Status == ScholarshipStatus.Accepted || offer.Status == ScholarshipStatus.Declined)
                throw new InvalidOperationException("Cannot rescind a finalized offer.");

            offer.Status = ScholarshipStatus.Rescinded;
            offer.RescindedByUserId = directorId;
            offer.RescindReason = dto.Reason;
            offer.RescindedDate = DateTime.UtcNow;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ScholarshipBudgetDto> GetBudgetStatsAsync(int bandId)
        {
            // Efficient Database Query for Budget using IUnitOfWork
            var stats = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId)
                .GroupBy(o => 1) // Group all to get aggregates
                .Select(g => new ScholarshipBudgetDto
                {
                    TotalBudget = 50000, // This should come from a BandSettings table
                    CommittedAmount = g.Where(o => o.Status == ScholarshipStatus.Accepted).Sum(o => o.ScholarshipAmount),
                    PendingAmount = g.Where(o => o.Status == ScholarshipStatus.Sent || o.Status == ScholarshipStatus.PendingApproval).Sum(o => o.ScholarshipAmount),
                })
                .FirstOrDefaultAsync() ?? new ScholarshipBudgetDto();

            stats.AvailableAmount = stats.TotalBudget - stats.CommittedAmount - stats.PendingAmount;
            return stats;
        }

        // Helper mapper
        private static ScholarshipOfferDto MapToDto(ScholarshipOffer offer) => new ScholarshipOfferDto
        {
            OfferId = offer.Id,
            StudentId = offer.StudentId,
            Status = offer.Status,
            ScholarshipAmount = offer.ScholarshipAmount,
            OfferType = offer.OfferType,
            CreatedAt = offer.CreatedAt,
            ApprovedAt = offer.ApprovedAt,
            ExpirationDate = offer.ExpirationDate,
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
                .Select(so => new ScholarshipOfferDto
                {
                    OfferId = so.Id,
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName,
                    BandId = so.BandId,
                    BandName = so.Band.BandName,
                    Status = so.Status,
                    ScholarshipAmount = so.ScholarshipAmount,
                    OfferType = so.OfferType,
                    CreatedAt = so.CreatedAt,
                    ApprovedAt = so.ApprovedAt,
                    ResponseDate = so.ResponseDate,
                    ExpirationDate = so.ExpirationDate,
                    Notes = so.Description,
                    Terms = so.Terms,
                    RescindReason = so.RescindReason,
                    CreatedByStaffName = so.CreatedByStaff != null ? so.CreatedByStaff.ApplicationUserId : "Unknown",
                    ApprovedByUserId = so.ApprovedByUserId,
                    RespondedByGuardianUserId = so.RespondedByGuardianUserId,
                    RequiresGuardianApproval = so.RequiresGuardianApproval
                })
                .ToListAsync();

            // 6. Return Overview
            decimal totalBudget = 50000m;

            return new ScholarshipOverviewDto
            {
                TotalOffers = summary?.TotalCount ?? 0,
                TotalAmount = summary?.TotalAmount ?? 0m,
                PendingCount = summary?.PendingCount ?? 0,
                ApprovedCount = summary?.ApprovedCount ?? 0,
                AcceptedCount = summary?.AcceptedCount ?? 0,
                DeclinedCount = summary?.DeclinedCount ?? 0,
                AvailableBudget = totalBudget - (summary?.TotalAmount ?? 0m),
                Offers = offers,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize,
                TotalPages = (int)Math.Ceiling((double)(summary?.TotalCount ?? 0) / filters.PageSize)
            };
        }

        public Task CheckExpirationsAsync() => Task.CompletedTask;
    
     
    }
}
