using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandEvent;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;
using Podium.Core.Entities;
using Podium.Core.Interfaces; // Updated
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.Interfaces;
using Podium.Core.Constants;

namespace Podium.Application.Services
{
    public class DirectorService : IDirectorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DirectorService> _logger;

        public DirectorService(IUnitOfWork unitOfWork, ILogger<DirectorService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DirectorDashboardDto?> GetDashboardAsync(string userId)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);

            if (band == null) return null;

            var bandId = band.Id;
            var now = DateTime.UtcNow;
            var oneWeekAgo = now.AddDays(-7);
            var oneMonthAgo = now.AddMonths(-1);

            var interestMetrics = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId)
                .GroupBy(si => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    LastWeek = g.Count(si => si.InterestedDate >= oneWeekAgo),
                    LastMonth = g.Count(si => si.InterestedDate >= oneMonthAgo)
                })
                .FirstOrDefaultAsync();

            var scholarshipSummary = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(so => so.BandId == bandId)
                .GroupBy(so => 1)
                .Select(g => new ScholarshipSummaryDto
                {
                    TotalOffersMade = g.Count(),
                    PendingOffers = g.Count(so => so.Status == ScholarshipStatus.PendingApproval),
                    AcceptedOffers = g.Count(so => so.Status == ScholarshipStatus.Accepted),
                    DeclinedOffers = g.Count(so => so.Status == ScholarshipStatus.Declined),
                    TotalCommittedAmount = g.Where(so => so.Status == ScholarshipStatus.Accepted).Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    AvailableBudget = band.ScholarshipBudget - (g.Where(so => so.Status == ScholarshipStatus.Accepted).Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m)
                })
                .FirstOrDefaultAsync() ?? new ScholarshipSummaryDto
                {
                    AvailableBudget = band.ScholarshipBudget
                };

            var upcomingEvents = await _unitOfWork.BandEvents.GetQueryable()
                .Where(e => e.BandId == bandId && e.EventDate >= now && !e.IsArchived)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .Select(e => new UpcomingEventDto
                {
                    EventId = e.Id,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    EventType = e.EventType,
                    RegisteredCount = e.Registrations.Count
                })
                .ToListAsync();

            var staffActivity = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.BandId == bandId && bs.IsActive)
                .Include(bs => bs.ApplicationUser)
                .OrderByDescending(bs => bs.LastActivityDate)
                .Take(5)
                .Select(bs => new StaffActivitySummaryDto
                {
                    StaffId = bs.Id,
                    StaffName = bs.FirstName + " " + bs.LastName,
                    Role = bs.Title ?? bs.Role,
                    ContactsInitiated = bs.TotalContactsInitiated,
                    OffersCreated = bs.TotalOffersCreated,
                    LastActiveDate = bs.LastActivityDate
                })
                .ToListAsync();

            var recentActivities = new List<DirectorRecentActivityDto>();

            var recentInterests = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId)
                .Include(si => si.Student)
                .OrderByDescending(si => si.InterestedDate)
                .Take(5)
                .Select(si => new DirectorRecentActivityDto
                {
                    ActivityType = "StudentInterest",
                    Description = "New student interest",
                    Timestamp = si.InterestedDate,
                    StudentName = si.Student.FirstName + " " + si.Student.LastName
                })
                .ToListAsync();

            recentActivities.AddRange(recentInterests);
            recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(10).ToList();

            var pendingContactRequests = await _unitOfWork.ContactRequests.GetQueryable()
                .CountAsync(cr => cr.BandId == bandId && cr.Status == "Pending");

            return new DirectorDashboardDto
            {
                BandId = bandId,
                BandName = band.BandName,
                TotalInterestedStudents = interestMetrics?.Total ?? 0,
                NewInterestedLastWeek = interestMetrics?.LastWeek ?? 0,
                NewInterestedLastMonth = interestMetrics?.LastMonth ?? 0,
                ScholarshipSummary = scholarshipSummary,
                UpcomingEvents = upcomingEvents,
                StaffActivity = staffActivity,
                RecentActivities = recentActivities,
                PendingContactRequests = pendingContactRequests
            };
        }

        public async Task<List<BandStaffDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);

            if (band == null) return new List<BandStaffDto>();

            var query = _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .Where(bs => bs.BandId == band.Id);

            if (isActive.HasValue)
                query = query.Where(bs => bs.IsActive == isActive.Value);

            query = sortBy?.ToLower() switch
            {
                "name" => query.OrderBy(bs => bs.LastName),
                "role" => query.OrderBy(bs => bs.Role),
                "joined" => query.OrderBy(bs => bs.JoinedDate),
                _ => query.OrderByDescending(bs => bs.JoinedDate)
            };

            return await query
                .Select(bs => new BandStaffDto
                {
                    BandStaffId = bs.Id,
                    BandId = bs.BandId,
                    ApplicationUserId = bs.ApplicationUserId,
                    FirstName = bs.FirstName,
                    LastName = bs.LastName,
                    Email = bs.ApplicationUser != null ? bs.ApplicationUser.Email : null,
                    Role = bs.Role,
                    Title = bs.Title,
                    IsActive = bs.IsActive,
                    JoinedDate = bs.JoinedDate,
                    CanViewStudents = bs.CanViewStudents,
                    CanRateStudents = bs.CanRateStudents,
                    CanSendOffers = bs.CanSendOffers,
                    CanManageEvents = bs.CanManageEvents,
                    CanManageStaff = bs.CanManageStaff,
                    CanContact = bs.CanContact,
                    CanMakeOffers = bs.CanMakeOffers,
                    CanViewFinancials = bs.CanViewFinancials,
                    TotalContactsInitiated = bs.TotalContactsInitiated,
                    TotalOffersCreated = bs.TotalOffersCreated,
                    LastActivityDate = bs.LastActivityDate
                })
                .ToListAsync();
        }

        public async Task<bool> CanAccessBandAsync(string userId, int bandId)
        {
            return await _unitOfWork.Bands.GetQueryable()
                .AnyAsync(b => b.Id == bandId && b.DirectorApplicationUserId == userId);
        }

        public async Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var interestTrend = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .GroupBy(si => new { si.InterestedDate.Year, si.InterestedDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Count = g.Count()
                })
                .OrderBy(mt => mt.Year)
                .ThenBy(mt => mt.Month)
                .ToListAsync();

            var scholarshipMetrics = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(so => so.BandId == bandId && so.CreatedAt >= startDate && so.CreatedAt <= endDate)
                .GroupBy(so => 1)
                .Select(g => new
                {
                    TotalOffered = g.Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    TotalAccepted = g.Where(so => so.Status == ScholarshipStatus.Accepted).Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    AverageOfferAmount = g.Average(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    AcceptanceRate = g.Count() > 0 ? (double)g.Count(so => so.Status == ScholarshipStatus.Accepted) / g.Count() * 100 : 0
                })
                .FirstOrDefaultAsync();

            var instrumentDistribution = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId)
                .GroupBy(si => si.Student.PrimaryInstrument)
                .Select(g => new InstrumentDistributionDto { Instrument = g.Key, Count = g.Count() })
                .OrderByDescending(id => id.Count)
                .ToListAsync();

            var geoDistribution = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId)
                .GroupBy(si => si.Student.State)
                .Select(g => new GeographicDistributionDto { State = g.Key, Count = g.Count() })
                .OrderByDescending(gd => gd.Count)
                .ToListAsync();

            return new BandAnalyticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                InterestTrend = interestTrend,
                TotalScholarshipOffered = scholarshipMetrics?.TotalOffered ?? 0m,
                TotalScholarshipAccepted = scholarshipMetrics?.TotalAccepted ?? 0m,
                AverageOfferAmount = scholarshipMetrics?.AverageOfferAmount ?? 0m,
                OfferAcceptanceRate = scholarshipMetrics?.AcceptanceRate ?? 0,
                InstrumentDistribution = instrumentDistribution,
                GeographicDistribution = geoDistribution
            };
        }

        public async Task<BandStaffDto> AddStaffMemberAsync(string directorUserId, CreateBandStaffDto request)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.Id == request.BandId && b.DirectorApplicationUserId == directorUserId && b.IsActive);

            if (band == null) throw new UnauthorizedAccessException("You do not have permission to add staff to this band");

            var existingStaff = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.BandId == request.BandId && bs.ApplicationUserId == request.ApplicationUserId);

            if (existingStaff != null && existingStaff.IsActive)
                throw new InvalidOperationException("This user is already a staff member of this band");

            if (existingStaff != null && !existingStaff.IsActive)
            {
                existingStaff.IsActive = true;
                existingStaff.Role = request.Role;
                existingStaff.CanContact = request.CanContact;
                existingStaff.CanMakeOffers = request.CanMakeOffers;
                existingStaff.CanViewFinancials = request.CanViewFinancials;
                _unitOfWork.BandStaff.Update(existingStaff);
                await _unitOfWork.SaveChangesAsync();

                return new BandStaffDto
                {
                    BandStaffId = existingStaff.Id,
                    BandId = existingStaff.BandId,
                    ApplicationUserId = existingStaff.ApplicationUserId,
                    Role = existingStaff.Role,
                    IsActive = existingStaff.IsActive,
                    JoinedDate = existingStaff.CreatedAt,
                    // Populate other fields...
                };
            }

            var newStaff = new BandStaff
            {
                BandId = request.BandId,
                ApplicationUserId = request.ApplicationUserId,
                Role = request.Role,
                CanContact = request.CanContact,
                CanMakeOffers = request.CanMakeOffers,
                CanViewFinancials = request.CanViewFinancials,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.BandStaff.AddAsync(newStaff);
            await _unitOfWork.SaveChangesAsync();

            return new BandStaffDto
            {
                BandStaffId = newStaff.Id,
                BandId = newStaff.BandId,
                ApplicationUserId = newStaff.ApplicationUserId,
                Role = newStaff.Role,
                IsActive = newStaff.IsActive,
                JoinedDate = newStaff.CreatedAt
                // Populate other fields...
            };
        }

        public async Task<bool> CanManageStaffAsync(string userId, int staffId)
        {
            var staffMember = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.Band)
                .FirstOrDefaultAsync(bs => bs.Id == staffId);

            if (staffMember == null) return false;
            return staffMember.Band.DirectorApplicationUserId == userId;
        }

        public async Task<BandStaffDto> UpdateStaffMemberAsync(int staffId, UpdateBandStaffDto request)
        {
            var staffMember = await _unitOfWork.BandStaff.GetByIdAsync(staffId);
            if (staffMember == null) throw new KeyNotFoundException($"Staff member {staffId} not found");

            staffMember.Role = request.Role;
            staffMember.CanContact = request.CanContact;
            staffMember.CanMakeOffers = request.CanMakeOffers;
            staffMember.CanViewFinancials = request.CanViewFinancials;
            staffMember.IsActive = request.IsActive;
            staffMember.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BandStaff.Update(staffMember);
            await _unitOfWork.SaveChangesAsync();

            return new BandStaffDto { BandStaffId = staffMember.Id /* ... map other fields */ };
        }

        public async Task RemoveStaffMemberAsync(int staffId)
        {
            var staffMember = await _unitOfWork.BandStaff.GetByIdAsync(staffId);
            if (staffMember == null) throw new KeyNotFoundException($"Staff member {staffId} not found");

            staffMember.IsActive = false;
            staffMember.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BandStaff.Update(staffMember);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);

            if (band == null) throw new KeyNotFoundException("Band not found for this director");

            var query = _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(so => so.BandId == band.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filters.Status) && Enum.TryParse<ScholarshipStatus>(filters.Status, true, out var statusEnum))
                query = query.Where(so => so.Status == statusEnum);

            if (filters.MinAmount.HasValue) query = query.Where(so => so.ScholarshipAmount >= filters.MinAmount.Value);
            if (filters.MaxAmount.HasValue) query = query.Where(so => so.ScholarshipAmount <= filters.MaxAmount.Value);
            if (filters.CreatedAfter.HasValue) query = query.Where(so => so.CreatedAt >= filters.CreatedAfter.Value);
            if (filters.CreatedBefore.HasValue) query = query.Where(so => so.CreatedAt <= filters.CreatedBefore.Value);

            var summary = await query
                .GroupBy(so => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    TotalAmount = g.Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    PendingCount = g.Count(so => so.Status == ScholarshipStatus.Pending),
                    ApprovedCount = g.Count(so => so.Status == ScholarshipStatus.Approved),
                    AcceptedCount = g.Count(so => so.Status == ScholarshipStatus.Accepted),
                    DeclinedCount = g.Count(so => so.Status == ScholarshipStatus.Declined)
                })
                .FirstOrDefaultAsync();

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
                    ScholarshipAmount = so.ScholarshipAmount,
                    Status = so.Status,
                    OfferType = so.OfferType,
                    CreatedAt = so.CreatedAt,
                    ApprovedAt = so.ApprovedAt,
                    ResponseDate = so.ResponseDate,
                    ExpirationDate = so.ExpirationDate,
                    Notes = so.Description,
                    CreatedByStaffName = so.CreatedByStaff.ApplicationUserId,
                    ApprovedByUserId = so.ApprovedByUserId,
                    RequiresGuardianApproval = so.RequiresGuardianApproval
                })
                .ToListAsync();

            return new ScholarshipOverviewDto
            {
                TotalOffers = summary?.TotalCount ?? 0,
                TotalAmount = summary?.TotalAmount ?? 0m,
                PendingCount = summary?.PendingCount ?? 0,
                ApprovedCount = summary?.ApprovedCount ?? 0,
                AcceptedCount = summary?.AcceptedCount ?? 0,
                DeclinedCount = summary?.DeclinedCount ?? 0,
                AvailableBudget = band.ScholarshipBudget - (summary?.TotalAmount ?? 0m),
                Offers = offers,
                CurrentPage = filters.Page,
                PageSize = filters.PageSize
            };
        }

        public async Task<bool> CanManageScholarshipAsync(string userId, int offerId)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(so => so.Band)
                .FirstOrDefaultAsync(so => so.Id == offerId);
            if (offer == null) return false;
            return offer.Band.DirectorApplicationUserId == userId;
        }

        public async Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(so => so.Student).Include(so => so.Band).Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null) throw new KeyNotFoundException($"Scholarship offer {offerId} not found");
            if (offer.Status != ScholarshipStatus.Pending) throw new InvalidOperationException($"Offer is not in Pending status");

            offer.Status = ScholarshipStatus.Accepted;
            offer.ApprovedDate = DateTime.UtcNow;
            offer.ApprovedByUserId = userId;
            if (!string.IsNullOrEmpty(notes)) offer.Description += $"\n[Director Approval] {notes}";

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();
            return new ScholarshipOfferDto { OfferId = offer.Id /* Map other fields */ };
        }

        public async Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(so => so.Student).Include(so => so.Band).Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null) throw new KeyNotFoundException($"Scholarship offer {offerId} not found");
            if (offer.Status == ScholarshipStatus.Declined || offer.Status == ScholarshipStatus.Rescinded)
                throw new InvalidOperationException($"Offer is already {offer.Status}");

            offer.Status = ScholarshipStatus.Rescinded;
            offer.RescindReason = reason;
            offer.RescindedDate = DateTime.UtcNow;
            offer.RescindedByUserId = userId;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();
            return new ScholarshipOfferDto { OfferId = offer.Id /* Map other fields */ };
        }

        public async Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);
            if (band == null) return new List<InterestedStudentDto>();

            var query = _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == band.Id)
                .Include(si => si.Student).ThenInclude(s => s.Videos)
                .Include(si => si.Student).ThenInclude(s => s.EventRegistrations)
                .Include(si => si.Student).ThenInclude(s => s.ContactLogs)
                .Include(si => si.Student).ThenInclude(s => s.ScholarshipOffers)
                .Include(si => si.Student).ThenInclude(s => s.Guardians)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filters.Instrument)) query = query.Where(si => si.Student.PrimaryInstrument == filters.Instrument);
            if (!string.IsNullOrEmpty(filters.SkillLevel)) query = query.Where(si => si.Student.SkillLevel == filters.SkillLevel);
            if (filters.GraduationYear.HasValue) query = query.Where(si => si.Student.GraduationYear == filters.GraduationYear.Value);
            if (filters.InterestedAfter.HasValue) query = query.Where(si => si.InterestedDate >= filters.InterestedAfter.Value);

            return await query
                .OrderByDescending(si => si.InterestedDate)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(si => new InterestedStudentDto
                {
                    StudentId = si.StudentId,
                    Name = si.Student.FirstName + " " + si.Student.LastName,
                    Email = si.Student.Email,
                    Phone = si.Student.PhoneNumber,
                    PrimaryInstrument = si.Student.PrimaryInstrument,
                    VideosUploaded = si.Student.Videos.Count,
                    EventsAttended = si.Student.EventRegistrations.Count(er => er.DidAttend),
                    HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == band.Id),
                    HasOffer = si.Student.ScholarshipOffers.Any(so => so.BandId == band.Id),
                    OfferStatus = si.Student.ScholarshipOffers.Where(so => so.BandId == band.Id).OrderByDescending(so => so.CreatedAt).Select(so => so.Status.ToString()).FirstOrDefault(),
                    HasGuardianLinked = si.Student.Guardians.Any()
                })
                .ToListAsync();
        }

        public async Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters)
        {
            var band = await _unitOfWork.Bands.GetQueryable()
                .FirstOrDefaultAsync(b => b.DirectorApplicationUserId == userId && b.IsActive);
            if (band == null) return new List<BandEventDto>();

            var query = _unitOfWork.BandEvents.GetQueryable().Where(e => e.BandId == band.Id);

            if (!filters.IncludeArchived) query = query.Where(e => !e.IsArchived);
            if (filters.StartDate.HasValue) query = query.Where(e => e.EventDate >= filters.StartDate.Value);
            if (filters.EndDate.HasValue) query = query.Where(e => e.EventDate <= filters.EndDate.Value);
            if (!string.IsNullOrEmpty(filters.EventType)) query = query.Where(e => e.EventType == filters.EventType);

            return await query
                .OrderBy(e => e.EventDate)
                .Select(e => new BandEventDto
                {
                    EventId = e.Id,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    // Map remaining fields...
                })
                .ToListAsync();
        }
    }
}