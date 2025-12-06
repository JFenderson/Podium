using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandEvent;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.BandStaff;

namespace Podium.Application.Services
{
    public class DirectorService : IDirectorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DirectorService> _logger;

        public DirectorService(ApplicationDbContext context, ILogger<DirectorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive dashboard for director.
        /// PERFORMANCE OPTIMIZATION: Single query using projections and strategic includes.
        /// Uses Select() to project only needed fields, avoiding loading full entity graphs.
        /// </summary>
        public async Task<DirectorDashboardDto?> GetDashboardAsync(string userId)
        {
            // First, get the director's band
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (band == null)
                return null;

            var bandId = band.BandId;
            var now = DateTime.UtcNow;
            var oneWeekAgo = now.AddDays(-7);
            var oneMonthAgo = now.AddMonths(-1);

            // OPTIMIZATION: Execute multiple aggregations in parallel using Task.WhenAll
            var dashboardTask = Task.Run(async () =>
            {
                // Student interest metrics - single query with multiple aggregations
                var interestMetrics = await _context.StudentInterests
                    .Where(si => si.BandId == bandId)
                    .GroupBy(si => 1) // Dummy grouping for aggregation
                    .Select(g => new
                    {
                        Total = g.Count(),
                        LastWeek = g.Count(si => si.InterestedDate >= oneWeekAgo),
                        LastMonth = g.Count(si => si.InterestedDate >= oneMonthAgo)
                    })
                    .FirstOrDefaultAsync();

                // Scholarship summary - optimized aggregation
                var scholarshipSummary = await _context.Offers
                    .Where(so => so.BandId == bandId)
                    .GroupBy(so => 1)
                    .Select(g => new ScholarshipSummaryDto
                    {
                        TotalOffersMade = g.Count(),
                        PendingOffers = g.Count(so => so.Status == "Pending" || so.Status == "Approved"),
                        AcceptedOffers = g.Count(so => so.Status == "Accepted"),
                        DeclinedOffers = g.Count(so => so.Status == "Declined"),
                        TotalCommittedAmount = g.Where(so => so.Status == "Accepted").Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                        AvailableBudget = band.ScholarshipBudget - (g.Where(so => so.Status == "Accepted" || so.Status == "Pending" || so.Status == "Approved").Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m),
                        BudgetUtilizationPercentage = band.ScholarshipBudget > 0
                            ? ((g.Where(so => so.Status == "Accepted" || so.Status == "Pending" || so.Status == "Approved").Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m) / band.ScholarshipBudget * 100)
                            : 0
                    })
                    .FirstOrDefaultAsync() ?? new ScholarshipSummaryDto();

                // Upcoming events - next 5 events
                var upcomingEvents = await _context.BandEvents
                    .Where(e => e.BandId == bandId && e.EventDate >= now && !e.IsArchived)
                    .OrderBy(e => e.EventDate)
                    .Take(5)
                    .Select(e => new UpcomingEventDto
                    {
                        EventId = e.BandEventId,
                        EventName = e.EventName,
                        EventDate = e.EventDate,
                        EventType = e.EventType,
                        RegisteredCount = e.Registrations.Count,
                        CapacityLimit = e.CapacityLimit ?? 0,
                        IsRegistrationOpen = e.IsRegistrationOpen
                    })
                    .ToListAsync();

                // Staff activity summary - top 5 most active staff
                var staffActivity = await _context.BandStaff
                    .Where(bs => bs.BandId == bandId && bs.IsActive)
                    .OrderByDescending(bs => bs.LastActivityDate)
                    .Take(5)
                    .Select(bs => new StaffActivitySummaryDto
                    {
                        StaffId = bs.BandStaffId,
                        StaffName = bs.ApplicationUserId, // Would join with Identity user table in real app
                        Role = bs.Role,
                        ContactsInitiated = bs.TotalContactsInitiated,
                        OffersCreated = bs.TotalOffersCreated,
                        LastActiveDate = bs.LastActivityDate
                    })
                    .ToListAsync();

                // Recent activities - last 10 significant events
                var recentActivities = new List<RecentActivityDto>();

                // Combine multiple activity sources
                var recentInterests = await _context.StudentInterests
                    .Where(si => si.BandId == bandId)
                    .OrderByDescending(si => si.InterestedDate)
                    .Take(5)
                    .Select(si => new RecentActivityDto
                    {
                        ActivityType = "StudentInterest",
                        Description = $"New student interest",
                        Timestamp = si.InterestedDate,
                        StudentName = si.Student.FirstName + " " + si.Student.LastName
                    })
                    .ToListAsync();

                var recentOffers = await _context.Offers
                    .Where(so => so.BandId == bandId && so.CreatedAt >= oneWeekAgo)
                    .OrderByDescending(so => so.CreatedAt)
                    .Take(5)
                    .Select(so => new RecentActivityDto
                    {
                        ActivityType = so.Status == "Accepted" ? "OfferAccepted" : "OfferCreated",
                        Description = so.Status == "Accepted"
                            ? $"Scholarship offer accepted"
                            : $"Scholarship offer created",
                        Timestamp = so.Status == "Accepted" ? so.ResponseDate ?? so.CreatedAt : so.CreatedAt,
                        StudentName = so.Student.FirstName + " " + so.Student.LastName,
                        StaffName = so.CreatedByStaff.ApplicationUserId
                    })
                    .ToListAsync();

                recentActivities.AddRange(recentInterests);
                recentActivities.AddRange(recentOffers);
                recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(10).ToList();

                // Video and contact request counts
                var videoStats = await _context.Videos
                    .Where(v => v.Student.StudentInterests.Any(si => si.BandId == bandId))
                    .GroupBy(v => 1)
                    .Select(g => new
                    {
                        Total = g.Count(),
                        AwaitingReview = g.Count(v => !v.IsReviewed)
                    })
                    .FirstOrDefaultAsync();

                var pendingContactRequests = await _context.ContactRequests
                    .Where(cr => cr.BandId == bandId && cr.Status == "Pending")
                    .CountAsync();

                return new DirectorDashboardDto
                {
                    BandId = bandId,
                    BandName = band.Name,
                    TotalInterestedStudents = interestMetrics?.Total ?? 0,
                    NewInterestedLastWeek = interestMetrics?.LastWeek ?? 0,
                    NewInterestedLastMonth = interestMetrics?.LastMonth ?? 0,
                    ScholarshipSummary = scholarshipSummary,
                    UpcomingEvents = upcomingEvents,
                    StaffActivity = staffActivity,
                    RecentActivities = recentActivities,
                    TotalVideosSubmitted = videoStats?.Total ?? 0,
                    VideosAwaitingReview = videoStats?.AwaitingReview ?? 0,
                    PendingContactRequests = pendingContactRequests
                };
            });

            return await dashboardTask;
        }

        public async Task<bool> CanAccessBandAsync(string userId, int bandId)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.BandId == bandId && b.ApplicationUserId == userId && b.IsActive);

            return band != null;
        }

        public async Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            // Student interest trend over time period
            var interestTrend = await _context.StudentInterests
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

            // Scholarship metrics
            var scholarshipMetrics = await _context.Offers
                .Where(so => so.BandId == bandId && so.CreatedAt >= startDate && so.CreatedAt <= endDate)
                .GroupBy(so => 1)
                .Select(g => new
                {
                    TotalOffered = g.Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    TotalAccepted = g.Where(so => so.Status == "Accepted").Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    AverageOfferAmount = g.Average(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    AcceptanceRate = g.Count() > 0 ? (double)g.Count(so => so.Status == "Accepted") / g.Count() * 100 : 0
                })
                .FirstOrDefaultAsync();

            // Instrument distribution
            var instrumentDistribution = await _context.StudentInterests
                .Where(si => si.BandId == bandId)
                .GroupBy(si => si.Student.PrimaryInstrument)
                .Select(g => new InstrumentDistributionDto
                {
                    Instrument = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(id => id.Count)
                .ToListAsync();

            // Geographic distribution
            var geoDistribution = await _context.StudentInterests
                .Where(si => si.BandId == bandId)
                .GroupBy(si => si.Student.State)
                .Select(g => new GeographicDistributionDto
                {
                    State = g.Key,
                    Count = g.Count()
                })
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
            // Verify director owns the band
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.BandId == request.BandId && b.ApplicationUserId == directorUserId && b.IsActive);

            if (band == null)
                throw new UnauthorizedAccessException("You do not have permission to add staff to this band");

            // Check if staff member already exists
            var existingStaff = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.BandId == request.BandId && bs.ApplicationUserId == request.ApplicationUserId);

            if (existingStaff != null && existingStaff.IsActive)
                throw new InvalidOperationException("This user is already a staff member of this band");

            // If exists but inactive, reactivate
            if (existingStaff != null && !existingStaff.IsActive)
            {
                existingStaff.IsActive = true;
                existingStaff.Role = request.Role;
                existingStaff.CanContact = request.CanContact;
                existingStaff.CanMakeOffers = request.CanMakeOffers;
                existingStaff.CanViewFinancials = request.CanViewFinancials;
                await _context.SaveChangesAsync();

                return new BandStaffDto
                {
                    BandStaffId = existingStaff.BandStaffId,
                    BandId = existingStaff.BandId,
                    ApplicationUserId = existingStaff.ApplicationUserId,
                    Role = existingStaff.Role,
                    CanContact = existingStaff.CanContact,
                    CanMakeOffers = existingStaff.CanMakeOffers,
                    CanViewFinancials = existingStaff.CanViewFinancials,
                    IsActive = existingStaff.IsActive,
                    JoinedDate = existingStaff.CreatedAt,
                    TotalContactsInitiated = existingStaff.TotalContactsInitiated,
                    TotalOffersCreated = existingStaff.TotalOffersCreated,
                    LastActivityDate = existingStaff.LastActivityDate
                };
            }

            // Create new staff member
            var newStaff = new BandStaff
            {
                BandId = request.BandId,
                ApplicationUserId = request.ApplicationUserId,
                Role = request.Role,
                CanContact = request.CanContact,
                CanMakeOffers = request.CanMakeOffers,
                CanViewFinancials = request.CanViewFinancials,
                IsActive = true,
                TotalContactsInitiated = 0,
                TotalOffersCreated = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BandStaff.Add(newStaff);
            await _context.SaveChangesAsync();

            return new BandStaffDto
            {
                BandStaffId = newStaff.BandStaffId,
                BandId = newStaff.BandId,
                ApplicationUserId = newStaff.ApplicationUserId,
                Role = newStaff.Role,
                CanContact = newStaff.CanContact,
                CanMakeOffers = newStaff.CanMakeOffers,
                CanViewFinancials = newStaff.CanViewFinancials,
                IsActive = newStaff.IsActive,
                JoinedDate = newStaff.CreatedAt,
                TotalContactsInitiated = newStaff.TotalContactsInitiated,
                TotalOffersCreated = newStaff.TotalOffersCreated,
                LastActivityDate = newStaff.LastActivityDate
            };
        }

        public async Task<bool> CanManageStaffAsync(string userId, int staffId)
        {
            var staffMember = await _context.BandStaff
                .Include(bs => bs.Band)
                .FirstOrDefaultAsync(bs => bs.BandStaffId == staffId);

            if (staffMember == null)
                return false;

            return staffMember.Band.ApplicationUserId == userId;
        }

        public async Task<BandStaffDto> UpdateStaffMemberAsync(int staffId, UpdateBandStaffDto request)
        {
            var staffMember = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.BandStaffId == staffId);

            if (staffMember == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            staffMember.Role = request.Role;
            staffMember.CanContact = request.CanContact;
            staffMember.CanMakeOffers = request.CanMakeOffers;
            staffMember.CanViewFinancials = request.CanViewFinancials;
            staffMember.IsActive = request.IsActive;
            staffMember.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new BandStaffDto
            {
                BandStaffId = staffMember.BandStaffId,
                BandId = staffMember.BandId,
                ApplicationUserId = staffMember.ApplicationUserId,
                Role = staffMember.Role,
                CanContact = staffMember.CanContact,
                CanMakeOffers = staffMember.CanMakeOffers,
                CanViewFinancials = staffMember.CanViewFinancials,
                IsActive = staffMember.IsActive,
                JoinedDate = staffMember.CreatedAt,
                TotalContactsInitiated = staffMember.TotalContactsInitiated,
                TotalOffersCreated = staffMember.TotalOffersCreated,
                LastActivityDate = staffMember.LastActivityDate
            };
        }

        public async Task RemoveStaffMemberAsync(int staffId)
        {
            var staffMember = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.BandStaffId == staffId);

            if (staffMember == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            staffMember.IsActive = false;
            staffMember.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<BandStaffDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (band == null)
                return new List<BandStaffDto>();

            var query = _context.BandStaff
                .Where(bs => bs.BandId == band.BandId);

            if (isActive.HasValue)
                query = query.Where(bs => bs.IsActive == isActive.Value);

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "name" => query.OrderBy(bs => bs.ApplicationUserId),
                "role" => query.OrderBy(bs => bs.Role),
                "joineddate" => query.OrderBy(bs => bs.CreatedAt),
                "lastactivity" => query.OrderByDescending(bs => bs.LastActivityDate),
                _ => query.OrderByDescending(bs => bs.CreatedAt)
            };

            return await query
                .Select(bs => new BandStaffDto
                {
                    BandStaffId = bs.BandStaffId,
                    BandId = bs.BandId,
                    ApplicationUserId = bs.ApplicationUserId,
                    Role = bs.Role,
                    CanContact = bs.CanContact,
                    CanMakeOffers = bs.CanMakeOffers,
                    CanViewFinancials = bs.CanViewFinancials,
                    IsActive = bs.IsActive,
                    JoinedDate = bs.CreatedAt,
                    TotalContactsInitiated = bs.TotalContactsInitiated,
                    TotalOffersCreated = bs.TotalOffersCreated,
                    LastActivityDate = bs.LastActivityDate
                })
                .ToListAsync();
        }

        public async Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (band == null)
                throw new KeyNotFoundException("Band not found for this director");

            var query = _context.Offers
                .Where(so => so.BandId == band.BandId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filters.Status))
                query = query.Where(so => so.Status == filters.Status);

            if (filters.MinAmount.HasValue)
                query = query.Where(so => so.ScholarshipAmount >= filters.MinAmount.Value);

            if (filters.MaxAmount.HasValue)
                query = query.Where(so => so.ScholarshipAmount <= filters.MaxAmount.Value);

            if (filters.CreatedAfter.HasValue)
                query = query.Where(so => so.CreatedAt >= filters.CreatedAfter.Value);

            if (filters.CreatedBefore.HasValue)
                query = query.Where(so => so.CreatedAt <= filters.CreatedBefore.Value);

            // Get summary statistics
            var summary = await query
                .GroupBy(so => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    TotalAmount = g.Sum(so => (decimal?)so.ScholarshipAmount) ?? 0m,
                    PendingCount = g.Count(so => so.Status == "Pending"),
                    ApprovedCount = g.Count(so => so.Status == "Approved"),
                    AcceptedCount = g.Count(so => so.Status == "Accepted"),
                    DeclinedCount = g.Count(so => so.Status == "Declined")
                })
                .FirstOrDefaultAsync();

            // Get paginated offers
            var offers = await query
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .OrderByDescending(so => so.CreatedAt)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(so => new ScholarshipOfferDto
                {
                    OfferId = so.OfferId,
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName,
                    BandId = so.BandId,
                    BandName = so.Band.Name,
                    ScholarshipAmount = so.ScholarshipAmount,
                    Status = so.Status,
                    OfferType = so.OfferType,
                    CreatedAt = so.CreatedAt,
                    ApprovedDate = so.ApprovedDate,
                    ResponseDate = so.ResponseDate,
                    ExpirationDate = so.ExpirationDate,
                    Notes = so.Notes,
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
            var offer = await _context.Offers
                .Include(so => so.Band)
                .FirstOrDefaultAsync(so => so.OfferId == offerId);

            if (offer == null)
                return false;

            return offer.Band.ApplicationUserId == userId;
        }

        public async Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes)
        {
            var offer = await _context.Offers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.OfferId == offerId);

            if (offer == null)
                throw new KeyNotFoundException($"Scholarship offer {offerId} not found");

            if (offer.Status != "Pending")
                throw new InvalidOperationException($"Offer is not in Pending status (current: {offer.Status})");

            offer.Status = "Approved";
            offer.ApprovedDate = DateTime.UtcNow;
            offer.ApprovedByUserId = userId;
            if (!string.IsNullOrEmpty(notes))
                offer.Notes += $"\n[Director Approval] {notes}";

            await _context.SaveChangesAsync();

            return new ScholarshipOfferDto
            {
                OfferId = offer.OfferId,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandId = offer.BandId,
                BandName = offer.Band.Name,
                ScholarshipAmount = offer.ScholarshipAmount,
                Status = offer.Status,
                OfferType = offer.OfferType,
                CreatedAt = offer.CreatedAt,
                ApprovedDate = offer.ApprovedDate,
                ResponseDate = offer.ResponseDate,
                ExpirationDate = offer.ExpirationDate,
                Notes = offer.Notes,
                CreatedByStaffName = offer.CreatedByStaff.ApplicationUserId,
                ApprovedByUserId = offer.ApprovedByUserId,
                RequiresGuardianApproval = offer.RequiresGuardianApproval
            };
        }

        public async Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason)
        {
            var offer = await _context.Offers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.OfferId == offerId);

            if (offer == null)
                throw new KeyNotFoundException($"Scholarship offer {offerId} not found");

            if (offer.Status == "Declined" || offer.Status == "Rescinded")
                throw new InvalidOperationException($"Offer is already {offer.Status}");

            offer.Status = "Rescinded";
            offer.RescindReason = reason;
            offer.RescindedDate = DateTime.UtcNow;
            offer.RescindedByUserId = userId;

            await _context.SaveChangesAsync();

            return new ScholarshipOfferDto
            {
                OfferId = offer.OfferId,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandId = offer.BandId,
                BandName = offer.Band.Name,
                ScholarshipAmount = offer.ScholarshipAmount,
                Status = offer.Status,
                OfferType = offer.OfferType,
                CreatedAt = offer.CreatedAt,
                ApprovedDate = offer.ApprovedDate,
                ResponseDate = offer.ResponseDate,
                ExpirationDate = offer.ExpirationDate,
                Notes = offer.Notes,
                CreatedByStaffName = offer.CreatedByStaff.ApplicationUserId,
                ApprovedByUserId = offer.ApprovedByUserId,
                RequiresGuardianApproval = offer.RequiresGuardianApproval,
                RescindReason = offer.RescindReason
            };
        }

        public async Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (band == null)
                return new List<InterestedStudentDto>();

            var query = _context.StudentInterests
                .Where(si => si.BandId == band.BandId)
                .Include(si => si.Student)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filters.Instrument))
                query = query.Where(si => si.Student.PrimaryInstrument == filters.Instrument);

            if (!string.IsNullOrEmpty(filters.SkillLevel))
                query = query.Where(si => si.Student.SkillLevel == filters.SkillLevel);

            if (filters.GraduationYear.HasValue)
                query = query.Where(si => si.Student.GraduationYear == filters.GraduationYear.Value);

            if (filters.InterestedAfter.HasValue)
                query = query.Where(si => si.InterestedDate >= filters.InterestedAfter.Value);

            var students = await query
                .OrderByDescending(si => si.InterestedDate)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(si => new InterestedStudentDto
                {
                    StudentId = si.StudentId,
                    Name = si.Student.FirstName + " " + si.Student.LastName,
                    Email = si.Student.Email,
                    Phone = si.Student.ContactPhone,
                    PrimaryInstrument = si.Student.PrimaryInstrument,
                    SkillLevel = si.Student.SkillLevel,
                    GraduationYear = si.Student.GraduationYear,
                    HighSchool = si.Student.HighSchool,
                    State = si.Student.State,
                    InterestedDate = si.InterestedDate,
                    VideosUploaded = si.Student.Videos.Count,
                    EventsAttended = si.Student.EventRegistrations.Count(er => er.DidAttend),
                    HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == band.BandId),
                    LastContactDate = si.Student.ContactLogs.Where(cl => cl.BandId == band.BandId).Max(cl => (DateTime?)cl.ContactDate),
                    HasOffer = si.Student.Offers.Any(so => so.BandId == band.BandId),
                    OfferStatus = si.Student.Offers.Where(so => so.BandId == band.BandId).OrderByDescending(so => so.CreatedAt).Select(so => so.Status).FirstOrDefault(),
                    HasGuardianLinked = si.Student.Guardians.Any(sg => sg.IsActive),
                    RequiresGuardianApproval = si.Student.RequiresGuardianApproval
                })
                .ToListAsync();

            return students;
        }

        public async Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (band == null)
                return new List<BandEventDto>();

            var query = _context.BandEvents
                .Where(e => e.BandId == band.BandId);

            if (!filters.IncludeArchived)
                query = query.Where(e => !e.IsArchived);

            if (filters.StartDate.HasValue)
                query = query.Where(e => e.EventDate >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                query = query.Where(e => e.EventDate <= filters.EndDate.Value);

            if (!string.IsNullOrEmpty(filters.EventType))
                query = query.Where(e => e.EventType == filters.EventType);

            return await query
                .OrderBy(e => e.EventDate)
                .Select(e => new BandEventDto
                {
                    EventId = e.BandEventId,
                    EventName = e.EventName,
                    Description = e.Description,
                    EventType = e.EventType,
                    EventDate = e.EventDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    CapacityLimit = e.CapacityLimit,
                    RegisteredCount = e.Registrations.Count,
                    AttendedCount = e.Registrations.Count(r => r.DidAttend),
                    IsRegistrationOpen = e.IsRegistrationOpen,
                    RegistrationDeadline = e.RegistrationDeadline,
                    IsVirtual = e.IsVirtual,
                    MeetingLink = e.MeetingLink,
                    CreatedDate = e.CreatedAt
                })
                .ToListAsync();
        }
    }
}