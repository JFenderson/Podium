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
                    Amount = so.ScholarshipAmount,
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

        // ==========================================
        // DASHBOARD
        // ==========================================

        public async Task<DirectorDashboardDto> GetDashboardAsync(string directorUserId, DirectorDashboardFiltersDto filters)
        {
            try
            {
                // Get director's band
                var director = await _unitOfWork.BandStaff.GetQueryable()
                    .FirstOrDefaultAsync(bs => bs.ApplicationUserId == directorUserId && bs.Role == "Director");

                if (director == null)
                    throw new UnauthorizedAccessException("Director profile not found");

                var bandId = director.BandId;
                var start = filters.StartDate ?? DateTime.UtcNow.AddDays(-30);
                var end = filters.EndDate ?? DateTime.UtcNow;

                // Load all dashboard data in parallel
                var metricsTask = await GetKeyMetricsAsync(directorUserId, start, end);
                var funnelTask = await GetRecruitmentFunnelAsync(bandId, start, end);
                var offersTask = await GetOffersOverviewAsync(bandId, start, end);
                var staffTask = await GetStaffPerformanceAsync(bandId, start, end);
                var approvalsTask = await GetPendingApprovalsAsync(bandId);
                var activityTask = await GetRecentActivityAsync(bandId, 20);


                return new DirectorDashboardDto
                {
                    KeyMetrics =  metricsTask,
                    RecruitmentFunnel =  funnelTask,
                    OffersOverview =  offersTask,
                    StaffPerformance =  staffTask,
                    PendingApprovals =  approvalsTask,
                    RecentActivity =  activityTask,
                    DateRangeStart = start,
                    DateRangeEnd = end
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading director dashboard for user {UserId}", directorUserId);
                throw;
            }
        }

        public async Task<DirectorKeyMetricsDto> GetKeyMetricsAsync(string directorUserId, DateTime startDate, DateTime endDate)
        {
            var director = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == directorUserId && bs.Role == "Director");

            if (director == null)
                throw new UnauthorizedAccessException("Director profile not found");

            var bandId = director.BandId;

            // Current period offers
            var currentOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            // Previous period for comparison
            var daysDiff = (endDate - startDate).Days;
            var previousStart = startDate.AddDays(-daysDiff);
            var previousOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId && o.CreatedAt >= previousStart && o.CreatedAt < startDate)
                .ToListAsync();

            var totalOffers = currentOffers.Count;
            var previousTotal = previousOffers.Count;
            var acceptedOffers = currentOffers.Count(o => o.Status == ScholarshipStatus.Accepted);
            var previousAccepted = previousOffers.Count(o => o.Status == ScholarshipStatus.Accepted);

            var acceptanceRate = totalOffers > 0 ? (double)acceptedOffers / totalOffers * 100 : 0;
            var previousAcceptanceRate = previousTotal > 0 ? (double)previousAccepted / previousTotal * 100 : 0;

            // Active recruiters (logged in within date range)
            var activeRecruiters = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.BandId == bandId &&
                            bs.IsActive &&
                            bs.Role != "Director" &&
                            bs.LastActivityDate >= startDate)
                .CountAsync();

            var previousActiveRecruiters = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.BandId == bandId &&
                            bs.IsActive &&
                            bs.Role != "Director" &&
                            bs.LastActivityDate >= previousStart &&
                            bs.LastActivityDate < startDate)
                .CountAsync();

            // Pipeline students (students with active contact requests or pending offers)
            var pipelineStudents = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == bandId &&
                            (cr.Status == "Pending" || cr.Status == "Approved"))
                .Select(cr => cr.StudentId)
                .Distinct()
                .CountAsync();

            var previousPipelineStudents = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == bandId &&
                            cr.RequestedDate >= previousStart &&
                            cr.RequestedDate < startDate &&
                            (cr.Status == "Pending" || cr.Status == "Approved"))
                .Select(cr => cr.StudentId)
                .Distinct()
                .CountAsync();

            // Budget calculations
            var totalBudgetAllocated = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.BandId == bandId && bs.IsActive)
                .SumAsync(bs => bs.BudgetAllocation ?? 0);

            var totalBudgetUsed = currentOffers
                .Where(o => o.Status == ScholarshipStatus.Accepted || o.Status == ScholarshipStatus.Sent)
                .Sum(o => o.ScholarshipAmount);

            var budgetUtilization = totalBudgetAllocated > 0
                ? (double)(totalBudgetUsed / totalBudgetAllocated) * 100
                : 0;

            return new DirectorKeyMetricsDto
            {
                TotalOffersSent = totalOffers,
                OffersSentChange = CalculatePercentageChange(totalOffers, previousTotal),
                AcceptanceRate = acceptanceRate,
                AcceptanceRateChange = acceptanceRate - previousAcceptanceRate,
                ActiveRecruiters = activeRecruiters,
                ActiveRecruitersChange = CalculatePercentageChange(activeRecruiters, previousActiveRecruiters),
                PipelineStudents = pipelineStudents,
                PipelineStudentsChange = CalculatePercentageChange(pipelineStudents, previousPipelineStudents),
                TotalBudgetAllocated = totalBudgetAllocated,
                TotalBudgetUsed = totalBudgetUsed,
                BudgetUtilization = budgetUtilization,
                AverageOfferAmount = totalOffers > 0 ? currentOffers.Average(o => o.ScholarshipAmount) : 0
            };
        }

        // ==========================================
        // RECRUITMENT FUNNEL
        // ==========================================

        public async Task<List<FunnelStageDto>> GetRecruitmentFunnelAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            // Stage 1: Contacted (students with contact requests)
            var contactedStudentIds = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == bandId && cr.RequestedDate >= startDate && cr.RequestedDate <= endDate)
                .Select(cr => cr.StudentId)
                .Distinct()
                .ToListAsync();

            var contactedCount = contactedStudentIds.Count;

            // Stage 2: Interested (students who responded or approved contact)
            var interestedCount = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == bandId &&
                            cr.RequestedDate >= startDate &&
                            cr.RequestedDate <= endDate &&
                            (cr.Status == "Approved"))
                .Select(cr => cr.StudentId)
                .Distinct()
                .CountAsync();

            // Stage 3: Offered (students who received offers)
            var offeredStudentIds = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Select(o => o.StudentId)
                .Distinct()
                .ToListAsync();

            var offeredCount = offeredStudentIds.Count;

            // Stage 4: Accepted (students who accepted offers)
            var acceptedCount = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate &&
                           o.Status == ScholarshipStatus.Accepted)
                .Select(o => o.StudentId)
                .Distinct()
                .CountAsync();

            // Stage 5: Enrolled (accepted students who confirmed enrollment)
            // Note: You may need to add an Enrollment entity/status
            var enrolledCount = (int)(acceptedCount * 0.85); // Placeholder: assume 85% enrollment rate

            var total = Math.Max(contactedCount, 1); // Prevent division by zero

            return new List<FunnelStageDto>
            {
                new FunnelStageDto
                {
                    Stage = "Contacted",
                    Count = contactedCount,
                    Percentage = 100,
                    ConversionRate = null
                },
                new FunnelStageDto
                {
                    Stage = "Interested",
                    Count = interestedCount,
                    Percentage = (double)interestedCount / total * 100,
                    ConversionRate = contactedCount > 0 ? (double)interestedCount / contactedCount * 100 : 0
                },
                new FunnelStageDto
                {
                    Stage = "Offered",
                    Count = offeredCount,
                    Percentage = (double)offeredCount / total * 100,
                    ConversionRate = interestedCount > 0 ? (double)offeredCount / interestedCount * 100 : 0
                },
                new FunnelStageDto
                {
                    Stage = "Accepted",
                    Count = acceptedCount,
                    Percentage = (double)acceptedCount / total * 100,
                    ConversionRate = offeredCount > 0 ? (double)acceptedCount / offeredCount * 100 : 0
                },
                new FunnelStageDto
                {
                    Stage = "Enrolled",
                    Count = enrolledCount,
                    Percentage = (double)enrolledCount / total * 100,
                    ConversionRate = acceptedCount > 0 ? (double)enrolledCount / acceptedCount * 100 : 0
                }
            };
        }

        public async Task<List<FunnelStudentDto>> GetFunnelStageStudentsAsync(int bandId, string stage, DateTime startDate, DateTime endDate)
        {
            // Implementation would return students in specific stage
            // For brevity, returning placeholder
            return new List<FunnelStudentDto>();
        }

        // ==========================================
        // OFFERS OVERVIEW
        // ==========================================

        public async Task<OffersOverviewDto> GetOffersOverviewAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Include(o => o.CreatedByStaff)
                .Where(o => o.BandId == bandId && o.CreatedAt >= startDate && o.CreatedAt <= endDate) // updated CreatedDate to CreatedAt
                .ToListAsync();

            // Group by month for time series
            var offersByMonth = offers
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month }) // updated CreatedDate to CreatedAt
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new OfferTimeSeriesDto
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),    
                    TotalOffers = g.Count(),
                    AcceptedOffers = g.Count(o => o.Status == ScholarshipStatus.Accepted),
                    DeclinedOffers = g.Count(o => o.Status == ScholarshipStatus.Declined),
                    AverageAmount = g.Average(o => o.ScholarshipAmount)
                })
                .ToList();

            // Breakdown by instrument
            var offersByInstrument = offers
                .GroupBy(o => o.Student.PrimaryInstrument)
                .Select(g => new OfferBreakdownDto
                {
                    Label = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / Math.Max(offers.Count, 1) * 100,
                    TotalAmount = g.Sum(o => o.ScholarshipAmount),
                    AverageAmount = g.Average(o => o.ScholarshipAmount) // updated Amount to ScholarshipAmount
                })
                .OrderByDescending(b => b.Count)
                .ToList();

            // Breakdown by status
            var offersByStatus = offers
                .GroupBy(o => o.Status)
                .Select(g => new OfferBreakdownDto
                {
                    Label = g.Key.ToString(),
                    Count = g.Count(),
                    Percentage = (double)g.Count() / Math.Max(offers.Count, 1) * 100
                })
                .ToList();

            // Breakdown by recruiter
            var offersByRecruiter = offers
                .GroupBy(o => new { o.CreatedByStaffId, StaffName = $"{o.CreatedByStaff.FirstName} {o.CreatedByStaff.LastName}" })
                .Select(g => new OfferBreakdownDto
                {
                    Label = g.Key.StaffName,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / Math.Max(offers.Count, 1) * 100,
                    TotalAmount = g.Sum(o => o.ScholarshipAmount),
                    AverageAmount = g.Average(o => o.ScholarshipAmount) // updated Amount to ScholarshipAmount
                })
                .OrderByDescending(b => b.Count)
                .ToList();

            return new OffersOverviewDto
            {
                OffersByMonth = offersByMonth,
                OffersByInstrument = offersByInstrument,
                OffersByStatus = offersByStatus,
                OffersByRecruiter = offersByRecruiter,
                TotalOffers = offers.Count,
                AcceptedOffers = offers.Count(o => o.Status == ScholarshipStatus.Accepted),
                PendingOffers = offers.Count(o => o.Status == ScholarshipStatus.Sent || o.Status == ScholarshipStatus.Pending),
                DeclinedOffers = offers.Count(o => o.Status == ScholarshipStatus.Declined),
                ExpiredOffers = offers.Count(o => o.Status == ScholarshipStatus.Expired)
            };
        }

        // ==========================================
        // STAFF PERFORMANCE
        // ==========================================

        public async Task<List<StaffPerformanceDto>> GetStaffPerformanceAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            // Get all staff first
            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .Where(bs => bs.BandId == bandId && bs.IsActive && bs.Role != "Director")
                .ToListAsync();

            // Get ALL offers for this band and date range (single query)
            var allOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate)
                .ToListAsync();

            // Get ALL contacts for this band and date range (single query)
            var allContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == bandId &&
                            cr.RequestedDate >= startDate &&
                            cr.RequestedDate <= endDate)
                .ToListAsync();

            // Now process in memory (no more DB queries)
            var performanceList = new List<StaffPerformanceDto>();

            foreach (var member in staff)
            {
                // Filter offers for this staff member (in memory)
                var offers = allOffers
                    .Where(o => o.CreatedByStaffId == member.Id)
                    .ToList();

                // Filter contacts for this staff member (in memory)
                var contacts = allContacts
                    .Where(cr => cr.RecruiterStaffId == member.Id)
                    .ToList();

                var totalOffers = offers.Count;
                var acceptedOffers = offers.Count(o => o.Status == ScholarshipStatus.Accepted);
                var acceptanceRate = totalOffers > 0 ? (double)acceptedOffers / totalOffers * 100 : 0;

                var totalContacts = contacts.Count;
                var respondedContacts = contacts.Count(c => c.Status == "Approved");
                var responseRate = totalContacts > 0 ? (double)respondedContacts / totalContacts * 100 : 0;

                var daysActive = member.LastActivityDate.HasValue
                    ? (DateTime.UtcNow - member.LastActivityDate.Value).Days
                    : 999;

                performanceList.Add(new StaffPerformanceDto
                {
                    StaffId = member.Id,
                    StaffName = $"{member.FirstName} {member.LastName}",
                    Role = member.Role,
                    Email = member.ApplicationUser?.Email ?? "",
                    OffersCreated = totalOffers,
                    OffersAccepted = acceptedOffers,
                    AcceptanceRate = acceptanceRate,
                    StudentsContacted = totalContacts,
                    StudentsResponded = respondedContacts,
                    ResponseRate = responseRate,
                    TotalBudgetAllocated = member.BudgetAllocation ?? 0,
                    AverageOfferAmount = totalOffers > 0 ? offers.Average(o => o.ScholarshipAmount) : 0,
                    LastActivityDate = member.LastActivityDate ?? DateTime.MinValue,
                    DaysActive = daysActive
                });
            }

            // Calculate rankings
            for (int i = 0; i < performanceList.Count; i++)
            {
                performanceList[i].PerformanceRank = i + 1;
                performanceList[i].AcceptanceRateRank = i + 1;
            }

            return performanceList;
        }




        public async Task<StaffDetailsDto> GetStaffDetailsAsync(int staffId)
        {
            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .Include(bs => bs.Band)
                .FirstOrDefaultAsync(bs => bs.Id == staffId);

            if (staff == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            return new StaffDetailsDto
            {
                StaffId = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.ApplicationUser?.Email ?? "",
                Role = staff.Role,
                Title = staff.Title,
                BandName = staff.Band?.BandName ?? "",
                IsActive = staff.IsActive,
                JoinedDate = staff.JoinedDate,
                LastActivityDate = staff.LastActivityDate,
                BudgetAllocation = staff.BudgetAllocation ?? 0,
                // Permissions
                CanContact = staff.CanContact,
                CanViewStudents = staff.CanViewStudents,
                CanMakeOffers = staff.CanMakeOffers,
                CanViewFinancials = staff.CanViewFinancials,
                CanRateStudents = staff.CanRateStudents,
                CanSendOffers = staff.CanSendOffers,
                CanManageEvents = staff.CanManageEvents,
                CanManageStaff = staff.CanManageStaff
            };
        }

        public async Task UpdateStaffBudgetAsync(int staffId, decimal newBudget, string updatedBy)
        {
            var staff = await _unitOfWork.BandStaff.GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            staff.BudgetAllocation = newBudget;
            staff.ModifiedBy = updatedBy;
            staff.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BandStaff.Update(staff);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Budget updated for staff {StaffId} to {Budget} by {UpdatedBy}",
                staffId, newBudget, updatedBy);
        }

        public async Task UpdateStaffPermissionsAsync(int staffId, BandStaffPermissionsDto permissions, string updatedBy)
        {
            var staff = await _unitOfWork.BandStaff.GetByIdAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            staff.CanContact = permissions.CanContact;
            staff.CanViewStudents = permissions.CanViewStudents;
            staff.CanMakeOffers = permissions.CanMakeOffers;
            staff.CanViewFinancials = permissions.CanViewFinancials;
            staff.CanRateStudents = permissions.CanRateStudents;
            staff.CanSendOffers = permissions.CanSendOffers;
            staff.CanManageEvents = permissions.CanManageEvents;
            staff.CanManageStaff = permissions.CanManageStaff;
            staff.ModifiedBy = updatedBy;
            staff.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BandStaff.Update(staff);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Permissions updated for staff {StaffId} by {UpdatedBy}", staffId, updatedBy);
        }

        // ==========================================
        // APPROVALS
        // ==========================================

        public async Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(int bandId)
        {
            // Get offers requiring director approval
            var pendingOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Include(o => o.CreatedByStaff)
                .Where(o => o.BandId == bandId &&
                           o.RequiresDirectorApproval &&
                           o.DirectorApprovalStatus == "Pending")
                .OrderBy(o => o.ExpirationDate)
                .ToListAsync();

            return pendingOffers.Select(o =>
            {
                var daysUntilExpiration = (o.ExpirationDate - DateTime.UtcNow).Days;
                   

                return new PendingApprovalDto
                {
                    ApprovalId = o.Id,
                    Type = "ScholarshipOffer",
                    StudentId = o.StudentId,
                    StudentName = $"{o.Student.FirstName} {o.Student.LastName}",
                    Instrument = o.Student.PrimaryInstrument,
                    Amount = o.ScholarshipAmount,
                    OfferType = o.OfferType,
                    Description = o.Description,
                    RequestedByStaffId = o.CreatedByStaffId,
                    RequestedByStaffName = $"{o.CreatedByStaff.FirstName} {o.CreatedByStaff.LastName}",
                    RequestDate = o.CreatedAt,
                    Urgency = daysUntilExpiration <= 3 ? "High" : daysUntilExpiration <= 7 ? "Medium" : "Low",
                    Reason = o.DirectorApprovalReason,
                    CanApprove = true,
                    CanDeny = true
                };
            }).ToList();
        }

        public async Task<bool> ApproveOfferAsync(int approvalId, string directorUserId, string? notes)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(approvalId);
            if (offer == null)
                throw new KeyNotFoundException($"Offer {approvalId} not found");

            var director = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == directorUserId && bs.Role == "Director");

            if (director == null)
                throw new UnauthorizedAccessException("Director profile not found");

            offer.DirectorApprovalStatus = "Approved";
            offer.DirectorApprovalDate = DateTime.UtcNow;
            offer.DirectorApprovalNotes = notes;
            offer.ApprovedByDirectorId = director.Id;
            offer.Status = ScholarshipStatus.Sent; // Change status to Sent after approval

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Offer {OfferId} approved by director {DirectorId}", approvalId, director.Id);

            // TODO: Send notification to staff member
            // TODO: Trigger SignalR update

            return true;
        }

        public async Task<bool> DenyOfferAsync(int approvalId, string directorUserId, string reason)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(approvalId);
            if (offer == null)
                throw new KeyNotFoundException($"Offer {approvalId} not found");

            var director = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == directorUserId && bs.Role == "Director");

            if (director == null)
                throw new UnauthorizedAccessException("Director profile not found");

            offer.DirectorApprovalStatus = "Denied";
            offer.DirectorApprovalDate = DateTime.UtcNow;
            offer.DirectorApprovalNotes = reason;
            offer.DeniedByDirectorId = director.Id;
            offer.Status = ScholarshipStatus.Draft; // Revert to draft status

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Offer {OfferId} denied by director {DirectorId}. Reason: {Reason}",
                approvalId, director.Id, reason);

            // TODO: Send notification to staff member
            // TODO: Trigger SignalR update

            return true;
        }

        // ==========================================
        // ACTIVITY
        // ==========================================

        public async Task<List<ActivityItemDto>> GetRecentActivityAsync(int bandId, int limit)
        {
            var activities = new List<ActivityItemDto>();

            // Get recent offers
            var recentOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Include(o => o.CreatedByStaff)
                .Where(o => o.BandId == bandId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var offer in recentOffers)
            {
                var activityType = offer.Status switch
                {
                    ScholarshipStatus.Accepted => "OfferAccepted",
                    ScholarshipStatus.Declined => "OfferDeclined",
                    _ => "OfferSent"
                };

                activities.Add(new ActivityItemDto
                {
                    Id = offer.Id,
                    Timestamp = offer.UpdatedAt ?? offer.CreatedAt,
                    ActivityType = activityType,
                    ActorType = "Staff",
                    ActorId = offer.CreatedByStaffId,
                    ActorName = $"{offer.CreatedByStaff.FirstName} {offer.CreatedByStaff.LastName}",
                    StudentId = offer.StudentId,
                    StudentName = $"{offer.Student.FirstName} {offer.Student.LastName}",
                    StaffId = offer.CreatedByStaffId,
                    StaffName = $"{offer.CreatedByStaff.FirstName} {offer.CreatedByStaff.LastName}",
                    OfferId = offer.Id,
                    Description = $"{offer.Student.FirstName} {offer.Student.LastName} - ${offer.ScholarshipAmount:N0} scholarship {offer.Status.ToString().ToLower()}",
                    Details = offer.OfferType
                });
            }

            // Get recent contact requests
            var recentContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student)
                .Include(cr => cr.RecruiterStaff)
                .Where(cr => cr.BandId == bandId)
                .OrderByDescending(cr => cr.RequestedDate)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var contact in recentContacts)
            {
                activities.Add(new ActivityItemDto
                {
                    Id = contact.Id,
                    Timestamp = contact.RequestedDate,
                    ActivityType = "ContactMade",
                    ActorType = "Staff",
                    ActorId = contact.RecruiterStaffId,
                    ActorName = $"{contact.RecruiterStaff.FirstName} {contact.RecruiterStaff.LastName}",
                    StudentId = contact.StudentId,
                    StudentName = $"{contact.Student.FirstName} {contact.Student.LastName}",
                    StaffId = contact.RecruiterStaffId,
                    StaffName = $"{contact.RecruiterStaff.FirstName} {contact.RecruiterStaff.LastName}",
                    Description = $"Contact request sent to {contact.Student.FirstName} {contact.Student.LastName}",
                    Details = $"Status: {contact.Status}"
                });
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }

        // ==========================================
        // EXPORT
        // ==========================================

        public async Task<byte[]> ExportDashboardAsync(ExportOptionsDto options)
        {
            // Implementation would generate CSV/Excel/PDF
            // For now, returning placeholder
            return Array.Empty<byte>();
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private double CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return ((double)(current - previous) / previous) * 100;
        }



    }
}