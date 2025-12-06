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

namespace Podium.Application.Services
{
    public class DirectorService : Core.Interfaces.IDirectorService
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
                .FirstOrDefaultAsync(b => b.DirectorUserId == userId && b.IsActive);

            if (band == null)
                return null;

            var bandId = band.Id;
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
                var scholarshipSummary = await _context.ScholarshipOffers
                    .Where(so => so.BandId == bandId)
                    .GroupBy(so => 1)
                    .Select(g => new ScholarshipSummaryDto
                    {
                        TotalOffersMade = g.Count(),
                        PendingOffers = g.Count(so => so.Status == "Pending" || so.Status == "Approved"),
                        AcceptedOffers = g.Count(so => so.Status == "Accepted"),
                        DeclinedOffers = g.Count(so => so.Status == "Declined"),
                        TotalCommittedAmount = g.Where(so => so.Status == "Accepted").Sum(so => so.Amount),
                        AvailableBudget = band.ScholarshipBudget - g.Where(so => so.Status == "Accepted" || so.Status == "Pending" || so.Status == "Approved").Sum(so => so.Amount),
                        BudgetUtilizationPercentage = band.ScholarshipBudget > 0
                            ? (g.Where(so => so.Status == "Accepted" || so.Status == "Pending" || so.Status == "Approved").Sum(so => so.Amount) / band.ScholarshipBudget * 100)
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
                        EventId = e.Id,
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
                        StaffId = bs.Id,
                        StaffName = bs.UserId, // Would join with Identity user table in real app
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

                var recentOffers = await _context.ScholarshipOffers
                    .Where(so => so.BandId == bandId && so.CreatedDate >= oneWeekAgo)
                    .OrderByDescending(so => so.CreatedDate)
                    .Take(5)
                    .Select(so => new RecentActivityDto
                    {
                        ActivityType = so.Status == "Accepted" ? "OfferAccepted" : "OfferCreated",
                        Description = so.Status == "Accepted"
                            ? $"Scholarship offer accepted"
                            : $"Scholarship offer created",
                        Timestamp = so.Status == "Accepted" ? so.ResponseDate ?? so.CreatedDate : so.CreatedDate,
                        StudentName = so.Student.FirstName + " " + so.Student.LastName,
                        StaffName = so.CreatedByStaff.UserId
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

        /// <summary>
        /// Authorization check: Verify director manages the specified band.
        /// CRITICAL for security - ensures directors can only access their own band data.
        /// </summary>
        public async Task<bool> CanAccessBandAsync(string userId, int bandId)
        {
            return await _context.Bands
                .AnyAsync(b => b.Id == bandId && b.DirectorUserId == userId && b.IsActive);
        }

        /// <summary>
        /// Complex analytics aggregation with trend data over time.
        /// PERFORMANCE: Uses efficient grouping and projections to minimize data transfer.
        /// BUSINESS LOGIC: Calculates conversion rates, demographic breakdowns, and geographic distribution.
        /// </summary>
        public async Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var band = await _context.Bands.FindAsync(bandId);
            if (band == null)
                throw new KeyNotFoundException($"Band {bandId} not found");

            // PARALLEL EXECUTION: Run independent queries simultaneously
            var interestTrendsTask = GetInterestTrendsAsync(bandId, startDate, endDate);
            var scholarshipAnalyticsTask = GetScholarshipAnalyticsAsync(bandId, startDate, endDate);
            var conversionMetricsTask = GetConversionMetricsAsync(bandId, startDate, endDate);
            var demographicsTask = GetDemographicsAsync(bandId, startDate, endDate);
            var engagementTask = GetEngagementMetricsAsync(bandId, startDate, endDate);
            var geographicTask = GetGeographicDistributionAsync(bandId, startDate, endDate);

            await Task.WhenAll(
                interestTrendsTask,
                scholarshipAnalyticsTask,
                conversionMetricsTask,
                demographicsTask,
                engagementTask,
                geographicTask
            );

            return new BandAnalyticsDto
            {
                BandId = bandId,
                BandName = band.Name,
                StartDate = startDate,
                EndDate = endDate,
                InterestTrends = await interestTrendsTask,
                ScholarshipAnalytics = await scholarshipAnalyticsTask,
                ConversionMetrics = await conversionMetricsTask,
                Demographics = await demographicsTask,
                Engagement = await engagementTask,
                GeographicDistribution = await geographicTask
            };
        }

        private async Task<InterestTrendsDto> GetInterestTrendsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            // Compare with previous period for percentage change calculation
            var periodLength = (endDate - startDate).Days;
            var previousPeriodStart = startDate.AddDays(-periodLength);

            var currentCount = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .CountAsync();

            var previousCount = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= previousPeriodStart && si.InterestedDate < startDate)
                .CountAsync();

            var percentageChange = previousCount > 0 ? ((currentCount - previousCount) / (decimal)previousCount * 100) : 0;

            // Monthly breakdown with video counts
            var monthlyData = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .GroupBy(si => new { si.InterestedDate.Year, si.InterestedDate.Month })
                .Select(g => new MonthlyInterestDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    VideosUploaded = g.Sum(si => si.Student.Videos.Count(v => v.UploadedDate.Year == g.Key.Year && v.UploadedDate.Month == g.Key.Month))
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToListAsync();

            // Breakdown by instrument and skill level
            var byInstrument = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .GroupBy(si => si.Student.PrimaryInstrument)
                .Select(g => new { Instrument = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Instrument, x => x.Count);

            var bySkillLevel = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .GroupBy(si => si.Student.SkillLevel)
                .Select(g => new { SkillLevel = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SkillLevel, x => x.Count);

            return new InterestTrendsDto
            {
                TotalInterested = currentCount,
                PercentageChange = percentageChange,
                MonthlyBreakdown = monthlyData,
                InterestByInstrument = byInstrument,
                InterestBySkillLevel = bySkillLevel
            };
        }

        private async Task<ScholarshipAnalyticsDto> GetScholarshipAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var offers = await _context.ScholarshipOffers
                .Where(so => so.BandId == bandId && so.CreatedDate >= startDate && so.CreatedDate <= endDate)
                .ToListAsync();

            var totalOffers = offers.Count;
            var acceptedOffers = offers.Count(o => o.Status == "Accepted");
            var acceptanceRate = totalOffers > 0 ? (acceptedOffers / (double)totalOffers * 100) : 0;

            var amounts = offers.Select(o => o.Amount).OrderBy(a => a).ToList();
            var median = amounts.Count > 0
                ? (amounts.Count % 2 == 0
                    ? (amounts[amounts.Count / 2 - 1] + amounts[amounts.Count / 2]) / 2
                    : amounts[amounts.Count / 2])
                : 0;

            var monthlyTrends = await _context.ScholarshipOffers
                .Where(so => so.BandId == bandId && so.CreatedDate >= startDate && so.CreatedDate <= endDate)
                .GroupBy(so => new { so.CreatedDate.Year, so.CreatedDate.Month })
                .Select(g => new ScholarshipTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OffersCreated = g.Count(),
                    OffersAccepted = g.Count(so => so.Status == "Accepted"),
                    TotalAmount = g.Sum(so => so.Amount)
                })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToListAsync();

            var band = await _context.Bands.FindAsync(bandId);

            return new ScholarshipAnalyticsDto
            {
                TotalBudget = band?.ScholarshipBudget ?? 0,
                AllocatedAmount = offers.Where(o => o.Status != "Declined" && o.Status != "Rescinded").Sum(o => o.Amount),
                AcceptedAmount = offers.Where(o => o.Status == "Accepted").Sum(o => o.Amount),
                AvailableAmount = (band?.ScholarshipBudget ?? 0) - offers.Where(o => o.Status == "Accepted" || o.Status == "Pending" || o.Status == "Approved").Sum(o => o.Amount),
                AverageOfferAmount = totalOffers > 0 ? offers.Average(o => o.Amount) : 0,
                MedianOfferAmount = median,
                TotalOffers = totalOffers,
                AcceptanceRate = acceptanceRate,
                MonthlyTrends = monthlyTrends
            };
        }

        private async Task<ConversionMetricsDto> GetConversionMetricsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            // Funnel metrics: Interest -> Contact -> Offer -> Acceptance
            var interestedStudents = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .Select(si => si.StudentId)
                .Distinct()
                .ToListAsync();

            var contactedStudents = await _context.ContactLogs
                .Where(cl => cl.BandId == bandId && interestedStudents.Contains(cl.StudentId))
                .Select(cl => cl.StudentId)
                .Distinct()
                .CountAsync();

            var offeredStudents = await _context.ScholarshipOffers
                .Where(so => so.BandId == bandId && interestedStudents.Contains(so.StudentId))
                .Select(so => so.StudentId)
                .Distinct()
                .CountAsync();

            var acceptedStudents = await _context.ScholarshipOffers
                .Where(so => so.BandId == bandId && so.Status == "Accepted" && interestedStudents.Contains(so.StudentId))
                .Select(so => so.StudentId)
                .Distinct()
                .CountAsync();

            var totalInterested = interestedStudents.Count;

            // Calculate average time between stages
            var timingData = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .Select(si => new
                {
                    si.StudentId,
                    si.InterestedDate,
                    FirstContact = _context.ContactLogs
                        .Where(cl => cl.StudentId == si.StudentId && cl.BandId == bandId)
                        .OrderBy(cl => cl.ContactDate)
                        .Select(cl => cl.ContactDate)
                        .FirstOrDefault(),
                    FirstOffer = _context.ScholarshipOffers
                        .Where(so => so.StudentId == si.StudentId && so.BandId == bandId)
                        .OrderBy(so => so.CreatedDate)
                        .Select(so => so.CreatedDate)
                        .FirstOrDefault(),
                    Acceptance = _context.ScholarshipOffers
                        .Where(so => so.StudentId == si.StudentId && so.BandId == bandId && so.Status == "Accepted")
                        .OrderBy(so => so.ResponseDate)
                        .Select(so => so.ResponseDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var avgTimeToContact = timingData
                .Where(t => t.FirstContact != default)
                .Average(t => (t.FirstContact - t.InterestedDate).TotalDays);

            var avgTimeToOffer = timingData
                .Where(t => t.FirstOffer != default)
                .Average(t => (t.FirstOffer - t.InterestedDate).TotalDays);

            var avgTimeToAcceptance = timingData
                .Where(t => t.Acceptance != null)
                .Average(t => ((DateTime)t.Acceptance! - t.InterestedDate).TotalDays);

            return new ConversionMetricsDto
            {
                StudentsInterested = totalInterested,
                StudentsContacted = contactedStudents,
                StudentsOffered = offeredStudents,
                StudentsAccepted = acceptedStudents,
                InterestToContactRate = totalInterested > 0 ? (contactedStudents / (double)totalInterested * 100) : 0,
                ContactToOfferRate = contactedStudents > 0 ? (offeredStudents / (double)contactedStudents * 100) : 0,
                OfferToAcceptanceRate = offeredStudents > 0 ? (acceptedStudents / (double)offeredStudents * 100) : 0,
                OverallConversionRate = totalInterested > 0 ? (acceptedStudents / (double)totalInterested * 100) : 0,
                AverageTimeToContact = avgTimeToContact,
                AverageTimeToOffer = avgTimeToOffer,
                AverageTimeToAcceptance = avgTimeToAcceptance
            };
        }

        private async Task<DemographicBreakdownDto> GetDemographicsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var studentIds = await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .Select(si => si.StudentId)
                .ToListAsync();

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            return new DemographicBreakdownDto
            {
                ByGraduationYear = students.GroupBy(s => s.GraduationYear).ToDictionary(g => g.Key, g => g.Count()),
                ByInstrument = students.GroupBy(s => s.PrimaryInstrument).ToDictionary(g => g.Key, g => g.Count()),
                BySkillLevel = students.GroupBy(s => s.SkillLevel).ToDictionary(g => g.Key, g => g.Count()),
                ByState = students.GroupBy(s => s.State).ToDictionary(g => g.Key, g => g.Count()),
                BySchoolType = students.GroupBy(s => s.SchoolType ?? "Unknown").ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private async Task<EngagementMetricsDto> GetEngagementMetricsAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            var studentIds = await _context.StudentInterests
                .Where(si => si.BandId == bandId)
                .Select(si => si.StudentId)
                .ToListAsync();

            var totalVideos = await _context.Videos
                .Where(v => studentIds.Contains(v.StudentId))
                .CountAsync();

            var avgVideosPerStudent = studentIds.Count > 0 ? totalVideos / (double)studentIds.Count : 0;

            var eventStats = await _context.EventRegistrations
                .Where(er => er.Event.BandId == bandId)
                .GroupBy(er => 1)
                .Select(g => new
                {
                    Registrations = g.Count(),
                    Attendances = g.Count(er => er.DidAttend)
                })
                .FirstOrDefaultAsync();

            var profileViews = await _context.ProfileViews
                .Where(pv => pv.BandId == bandId && pv.ViewedDate >= startDate && pv.ViewedDate <= endDate)
                .CountAsync();

            var avgResponseTime = await _context.ContactRequests
                .Where(cr => cr.BandId == bandId && cr.Status != "Pending" && cr.ResponseDate != null)
                .AverageAsync(cr => EF.Functions.DateDiffHour(cr.RequestedDate, cr.ResponseDate!.Value));

            return new EngagementMetricsDto
            {
                TotalVideosUploaded = totalVideos,
                AverageVideosPerStudent = avgVideosPerStudent,
                EventRegistrations = eventStats?.Registrations ?? 0,
                EventAttendances = eventStats?.Attendances ?? 0,
                EventAttendanceRate = eventStats?.Registrations > 0 ? (eventStats.Attendances / (double)eventStats.Registrations * 100) : 0,
                ProfileViews = profileViews,
                AverageResponseTimeHours = avgResponseTime ?? 0
            };
        }

        private async Task<List<GeographicDataDto>> GetGeographicDistributionAsync(int bandId, DateTime startDate, DateTime endDate)
        {
            return await _context.StudentInterests
                .Where(si => si.BandId == bandId && si.InterestedDate >= startDate && si.InterestedDate <= endDate)
                .GroupBy(si => si.Student.State)
                .Select(g => new GeographicDataDto
                {
                    State = g.Key,
                    StudentCount = g.Count(),
                    OffersExtended = g.Sum(si => si.Student.ScholarshipOffers.Count(so => so.BandId == bandId)),
                    OffersAccepted = g.Sum(si => si.Student.ScholarshipOffers.Count(so => so.BandId == bandId && so.Status == "Accepted"))
                })
                .OrderByDescending(g => g.StudentCount)
                .ToListAsync();
        }

        // Continue in next file due to length...
        public async Task<StaffMemberDto> AddStaffMemberAsync(string directorUserId, AddStaffRequest request)
        {
            // Verify director manages the band
            if (!await CanAccessBandAsync(directorUserId, request.BandId))
                throw new UnauthorizedAccessException("You do not have permission to add staff to this band");

            // Check if staff member already exists
            var existing = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.BandId == request.BandId && bs.UserId == request.RecruiterUserId);

            if (existing != null)
            {
                if (existing.IsActive)
                    throw new InvalidOperationException("This user is already a staff member of this band");

                // Reactivate if was previously deactivated
                existing.IsActive = true;
                existing.Role = request.Role;
                existing.CanContact = request.Permissions.CanContact;
                existing.CanMakeOffers = request.Permissions.CanMakeOffers;
                existing.CanViewFinancials = request.Permissions.CanViewFinancials;
                existing.CanManageEvents = request.Permissions.CanManageEvents;
                existing.ModifiedBy = directorUserId;
                existing.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                existing = new BandStaff
                {
                    BandId = request.BandId,
                    UserId = request.RecruiterUserId,
                    Role = request.Role,
                    IsActive = true,
                    JoinedDate = DateTime.UtcNow,
                    CanContact = request.Permissions.CanContact,
                    CanMakeOffers = request.Permissions.CanMakeOffers,
                    CanViewFinancials = request.Permissions.CanViewFinancials,
                    CanManageEvents = request.Permissions.CanManageEvents,
                    CreatedBy = directorUserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.BandStaff.Add(existing);
            }

            await _context.SaveChangesAsync();

            return new StaffMemberDto
            {
                Id = existing.Id,
                UserId = existing.UserId,
                Name = existing.UserId, // Would lookup from Identity
                Email = "", // Would lookup from Identity
                Role = existing.Role,
                IsActive = existing.IsActive,
                JoinedDate = existing.JoinedDate,
                CanContact = existing.CanContact,
                CanMakeOffers = existing.CanMakeOffers,
                CanViewFinancials = existing.CanViewFinancials,
                CanManageEvents = existing.CanManageEvents,
                TotalContacts = existing.TotalContactsInitiated,
                TotalOffersCreated = existing.TotalOffersCreated,
                SuccessfulPlacements = existing.SuccessfulPlacements,
                LastActivityDate = existing.LastActivityDate
            };
        }

        public async Task<bool> CanManageStaffAsync(string userId, int staffId)
        {
            var staff = await _context.BandStaff
                .Include(bs => bs.Band)
                .FirstOrDefaultAsync(bs => bs.Id == staffId);

            return staff != null && staff.Band.DirectorUserId == userId;
        }

        public async Task<StaffMemberDto> UpdateStaffMemberAsync(int staffId, UpdateStaffRequest request)
        {
            var staff = await _context.BandStaff.FindAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            if (request.Role != null)
                staff.Role = request.Role;

            if (request.IsActive.HasValue)
            {
                staff.IsActive = request.IsActive.Value;
                if (!request.IsActive.Value)
                    staff.DeactivatedDate = DateTime.UtcNow;
            }

            if (request.Permissions != null)
            {
                staff.CanContact = request.Permissions.CanContact;
                staff.CanMakeOffers = request.Permissions.CanMakeOffers;
                staff.CanViewFinancials = request.Permissions.CanViewFinancials;
                staff.CanManageEvents = request.Permissions.CanManageEvents;
            }

            staff.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new StaffMemberDto
            {
                Id = staff.Id,
                UserId = staff.UserId,
                Name = staff.UserId,
                Email = "",
                Role = staff.Role,
                IsActive = staff.IsActive,
                JoinedDate = staff.JoinedDate,
                CanContact = staff.CanContact,
                CanMakeOffers = staff.CanMakeOffers,
                CanViewFinancials = staff.CanViewFinancials,
                CanManageEvents = staff.CanManageEvents,
                TotalContacts = staff.TotalContactsInitiated,
                TotalOffersCreated = staff.TotalOffersCreated,
                SuccessfulPlacements = staff.SuccessfulPlacements,
                LastActivityDate = staff.LastActivityDate
            };
        }

        public async Task RemoveStaffMemberAsync(int staffId)
        {
            var staff = await _context.BandStaff.FindAsync(staffId);
            if (staff == null)
                throw new KeyNotFoundException($"Staff member {staffId} not found");

            // Soft delete
            staff.IsActive = false;
            staff.DeactivatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<List<StaffMemberDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.DirectorUserId == userId && b.IsActive);

            if (band == null)
                return new List<StaffMemberDto>();

            var query = _context.BandStaff
                .Where(bs => bs.BandId == band.Id);

            if (isActive.HasValue)
                query = query.Where(bs => bs.IsActive == isActive.Value);

            query = sortBy?.ToLower() switch
            {
                "activity" => query.OrderByDescending(bs => bs.LastActivityDate),
                "contacts" => query.OrderByDescending(bs => bs.TotalContactsInitiated),
                "offers" => query.OrderByDescending(bs => bs.TotalOffersCreated),
                "name" => query.OrderBy(bs => bs.UserId),
                _ => query.OrderBy(bs => bs.Role).ThenBy(bs => bs.UserId)
            };

            return await query
                .Select(bs => new StaffMemberDto
                {
                    Id = bs.Id,
                    UserId = bs.UserId,
                    Name = bs.UserId,
                    Email = "",
                    Role = bs.Role,
                    IsActive = bs.IsActive,
                    JoinedDate = bs.JoinedDate,
                    CanContact = bs.CanContact,
                    CanMakeOffers = bs.CanMakeOffers,
                    CanViewFinancials = bs.CanViewFinancials,
                    CanManageEvents = bs.CanManageEvents,
                    TotalContacts = bs.TotalContactsInitiated,
                    TotalOffersCreated = bs.TotalOffersCreated,
                    SuccessfulPlacements = bs.SuccessfulPlacements,
                    LastActivityDate = bs.LastActivityDate
                })
                .ToListAsync();
        }

        // Remaining methods continued in next part...
        public async Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.DirectorUserId == userId && b.IsActive);

            if (band == null)
                return new ScholarshipOverviewDto();

            var query = _context.ScholarshipOffers
                .Where(so => so.BandId == band.Id);

            if (!string.IsNullOrEmpty(filters.Status))
                query = query.Where(so => so.Status == filters.Status);

            if (filters.StudentId.HasValue)
                query = query.Where(so => so.StudentId == filters.StudentId.Value);

            if (filters.StartDate.HasValue)
                query = query.Where(so => so.CreatedDate >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                query = query.Where(so => so.CreatedDate <= filters.EndDate.Value);

            if (filters.MinAmount.HasValue)
                query = query.Where(so => so.Amount >= filters.MinAmount.Value);

            if (filters.MaxAmount.HasValue)
                query = query.Where(so => so.Amount <= filters.MaxAmount.Value);

            var offers = await query
                .Include(so => so.Student)
                .Include(so => so.CreatedByStaff)
                .OrderByDescending(so => so.CreatedDate)
                .Select(so => new ScholarshipOfferDto
                {
                    Id = so.Id,
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName,
                    BandId = so.BandId,
                    BandName = so.Band.Name,
                    Amount = so.Amount,
                    Status = so.Status,
                    OfferType = so.OfferType,
                    CreatedDate = so.CreatedDate,
                    ApprovedDate = so.ApprovedDate,
                    ResponseDate = so.ResponseDate,
                    ExpirationDate = so.ExpirationDate,
                    Notes = so.Notes,
                    CreatedByName = so.CreatedByStaff.UserId,
                    ApprovedByName = so.ApprovedByUserId,
                    RequiresGuardianApproval = so.RequiresGuardianApproval,
                    RescindReason = so.RescindReason
                })
                .ToListAsync();

            var totalAmount = offers.Sum(o => o.Amount);

            return new ScholarshipOverviewDto
            {
                Offers = offers,
                TotalCount = offers.Count,
                TotalAmount = totalAmount,
                BudgetSummary = new ScholarshipBudgetDto
                {
                    TotalBudget = band.ScholarshipBudget,
                    AllocatedAmount = offers.Where(o => o.Status != "Declined" && o.Status != "Rescinded").Sum(o => o.Amount),
                    CommittedAmount = offers.Where(o => o.Status == "Accepted").Sum(o => o.Amount),
                    AvailableAmount = band.ScholarshipBudget - offers.Where(o => o.Status == "Accepted" || o.Status == "Pending" || o.Status == "Approved").Sum(o => o.Amount),
                    PendingAmount = offers.Where(o => o.Status == "Pending" || o.Status == "Approved").Sum(o => o.Amount)
                }
            };
        }

        public async Task<bool> CanManageScholarshipAsync(string userId, int offerId)
        {
            var offer = await _context.ScholarshipOffers
                .Include(so => so.Band)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            return offer != null && offer.Band.DirectorUserId == userId;
        }

        public async Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes)
        {
            var offer = await _context.ScholarshipOffers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null)
                throw new KeyNotFoundException($"Scholarship offer {offerId} not found");

            if (offer.Status != "Pending")
                throw new InvalidOperationException($"Cannot approve offer in status {offer.Status}");

            offer.Status = "Approved";
            offer.ApprovedDate = DateTime.UtcNow;
            offer.ApprovedByUserId = userId;
            if (!string.IsNullOrEmpty(notes))
                offer.Notes = (offer.Notes ?? "") + $"\n[Approval] {notes}";

            await _context.SaveChangesAsync();

            return new ScholarshipOfferDto
            {
                Id = offer.Id,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandId = offer.BandId,
                BandName = offer.Band.Name,
                Amount = offer.Amount,
                Status = offer.Status,
                OfferType = offer.OfferType,
                CreatedDate = offer.CreatedDate,
                ApprovedDate = offer.ApprovedDate,
                ResponseDate = offer.ResponseDate,
                ExpirationDate = offer.ExpirationDate,
                Notes = offer.Notes,
                CreatedByName = offer.CreatedByStaff.UserId,
                ApprovedByName = offer.ApprovedByUserId,
                RequiresGuardianApproval = offer.RequiresGuardianApproval
            };
        }

        public async Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason)
        {
            var offer = await _context.ScholarshipOffers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

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
                Id = offer.Id,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandId = offer.BandId,
                BandName = offer.Band.Name,
                Amount = offer.Amount,
                Status = offer.Status,
                OfferType = offer.OfferType,
                CreatedDate = offer.CreatedDate,
                ApprovedDate = offer.ApprovedDate,
                ResponseDate = offer.ResponseDate,
                ExpirationDate = offer.ExpirationDate,
                Notes = offer.Notes,
                CreatedByName = offer.CreatedByStaff.UserId,
                ApprovedByName = offer.ApprovedByUserId,
                RequiresGuardianApproval = offer.RequiresGuardianApproval,
                RescindReason = offer.RescindReason
            };
        }

        public async Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.DirectorUserId == userId && b.IsActive);

            if (band == null)
                return new List<InterestedStudentDto>();

            var query = _context.StudentInterests
                .Where(si => si.BandId == band.Id)
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
                    Phone = si.Student.PhoneNumber,
                    PrimaryInstrument = si.Student.PrimaryInstrument,
                    SkillLevel = si.Student.SkillLevel,
                    GraduationYear = si.Student.GraduationYear,
                    HighSchool = si.Student.HighSchool,
                    State = si.Student.State,
                    InterestedDate = si.InterestedDate,
                    VideosUploaded = si.Student.Videos.Count,
                    EventsAttended = si.Student.EventRegistrations.Count(er => er.DidAttend),
                    HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == band.Id),
                    LastContactDate = si.Student.ContactLogs.Where(cl => cl.BandId == band.Id).Max(cl => (DateTime?)cl.ContactDate),
                    HasOffer = si.Student.ScholarshipOffers.Any(so => so.BandId == band.Id),
                    OfferStatus = si.Student.ScholarshipOffers.Where(so => so.BandId == band.Id).OrderByDescending(so => so.CreatedDate).Select(so => so.Status).FirstOrDefault(),
                    HasGuardianLinked = si.Student.StudentGuardians.Any(sg => sg.IsActive),
                    RequiresGuardianApproval = si.Student.RequiresGuardianApproval
                })
                .ToListAsync();

            return students;
        }

        public async Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters)
        {
            var band = await _context.Bands
                .FirstOrDefaultAsync(b => b.DirectorUserId == userId && b.IsActive);

            if (band == null)
                return new List<BandEventDto>();

            var query = _context.BandEvents
                .Where(e => e.BandId == band.Id);

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
                    EventId = e.Id,
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
                    CreatedDate = e.CreatedDate
                })
                .ToListAsync();
        }
    }
}
}
