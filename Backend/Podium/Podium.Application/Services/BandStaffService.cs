using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Interfaces;

namespace Podium.Application.Services
{
    public class BandStaffService : IBandStaffService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BandStaffService> _logger;

        public BandStaffService(
            IUnitOfWork unitOfWork,
            ILogger<BandStaffService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BandStaffDashboardDto> GetDashboardAsync(string staffUserId, BandStaffDashboardFiltersDto filters)
        {
            try
            {
                var staff = await GetStaffMemberAsync(staffUserId);
                var start = filters.StartDate ?? DateTime.UtcNow.AddDays(-30);
                var end = filters.EndDate ?? DateTime.UtcNow;

                // Call methods sequentially to avoid DbContext concurrency
                var metrics = await GetPersonalMetricsAsync(staffUserId, start, end);
                var students = await GetMyStudentsAsync(staffUserId, filters.ContactStatus);
                var performance = await GetMyPerformanceAsync(staffUserId, start, end);
                var activity = await GetMyActivityAsync(staffUserId, 10);
                var tasks = await GetMyPendingTasksAsync(staffUserId);

                return new BandStaffDashboardDto
                {
                    PersonalMetrics = metrics,
                    MyStudents = students,
                    Performance = performance,
                    RecentActivity = activity,
                    PendingTasks = tasks,
                    DateRangeStart = start,
                    DateRangeEnd = end
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading band staff dashboard for user {UserId}", staffUserId);
                throw;
            }
        }

        public async Task<BandStaffPersonalMetricsDto> GetPersonalMetricsAsync(string staffUserId, DateTime startDate, DateTime endDate)
        {
            var staff = await GetStaffMemberAsync(staffUserId);

            // Get current period offers
            var currentOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate)
                .ToListAsync();

            // Get previous period for comparison
            var daysDiff = (endDate - startDate).Days;
            var previousStart = startDate.AddDays(-daysDiff);
            var previousOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id &&
                           o.CreatedAt >= previousStart &&
                           o.CreatedAt < startDate)
                .ToListAsync();

            // Calculate offer metrics
            var offersCreated = currentOffers.Count;
            var offersAccepted = currentOffers.Count(o => o.Status == ScholarshipStatus.Accepted);
            var acceptanceRate = offersCreated > 0 ? (double)offersAccepted / offersCreated * 100 : 0;

            var previousAccepted = previousOffers.Count(o => o.Status == ScholarshipStatus.Accepted);
            var previousAcceptanceRate = previousOffers.Count > 0 ? (double)previousAccepted / previousOffers.Count * 100 : 0;

            // Get contact metrics
            var currentContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id &&
                            cr.RequestedDate >= startDate &&
                            cr.RequestedDate <= endDate)
                .ToListAsync();

            var previousContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id &&
                            cr.RequestedDate >= previousStart &&
                            cr.RequestedDate < startDate)
                .ToListAsync();

            var studentsContacted = currentContacts.Count;
            var studentsResponded = currentContacts.Count(c => c.Status == "Approved");
            var responseRate = studentsContacted > 0 ? (double)studentsResponded / studentsContacted * 100 : 0;

            var previousResponded = previousContacts.Count(c => c.Status == "Approved");
            var previousResponseRate = previousContacts.Count > 0 ? (double)previousResponded / previousContacts.Count * 100 : 0;

            // Budget calculations
            var budgetAllocated = staff.BudgetAllocation ?? 0;
            var budgetUsed = currentOffers
                .Where(o => o.Status == ScholarshipStatus.Accepted || o.Status == ScholarshipStatus.Sent)
                .Sum(o => o.ScholarshipAmount);
            var budgetRemaining = budgetAllocated - budgetUsed;
            var budgetUtilization = budgetAllocated > 0 ? (double)(budgetUsed / budgetAllocated) * 100 : 0;

            // Activity
            var daysSinceLastActivity = staff.LastActivityDate.HasValue
                ? (DateTime.UtcNow - staff.LastActivityDate.Value).Days
                : 999;

            // Ratings given
            var ratingsGiven = await _unitOfWork.StudentRatings.GetQueryable()
                .CountAsync(r => r.BandStaffId == staff.Id &&
                               r.CreatedAt >= startDate &&
                               r.CreatedAt <= endDate);

            // Get rankings
            var allStaff = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.BandId == staff.BandId && bs.IsActive && bs.Role != "Director")
                .Select(bs => new { bs.Id, bs.TotalOffersCreated, bs.SuccessfulPlacements })
                .ToListAsync();

            var myRankByOffers = allStaff
                .OrderByDescending(s => s.TotalOffersCreated)
                .ToList()
                .FindIndex(s => s.Id == staff.Id) + 1;

            var myRankByAcceptance = allStaff
                .OrderByDescending(s => s.SuccessfulPlacements)
                .ToList()
                .FindIndex(s => s.Id == staff.Id) + 1;

            return new BandStaffPersonalMetricsDto
            {
                OffersCreated = offersCreated,
                OffersAccepted = offersAccepted,
                AcceptanceRate = acceptanceRate,
                AcceptanceRateChange = acceptanceRate - previousAcceptanceRate,

                StudentsContacted = studentsContacted,
                StudentsResponded = studentsResponded,
                ResponseRate = responseRate,
                ResponseRateChange = responseRate - previousResponseRate,

                BudgetAllocated = budgetAllocated,
                BudgetUsed = budgetUsed,
                BudgetRemaining = budgetRemaining,
                BudgetUtilization = budgetUtilization,

                DaysSinceLastActivity = daysSinceLastActivity,
                RatingsGiven = ratingsGiven,
                AverageOfferAmount = (double)(offersCreated > 0 ? currentOffers.Average(o => o.ScholarshipAmount) : 0),

                MyRankByOffers = myRankByOffers > 0 ? myRankByOffers : null,
                MyRankByAcceptance = myRankByAcceptance > 0 ? myRankByAcceptance : null,
                TotalStaff = allStaff.Count
            };
        }


        private async Task<Podium.Core.Entities.BandStaff> GetStaffMemberAsync(string userId)
        {
            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId && bs.IsActive);

            if (staff == null)
                throw new UnauthorizedAccessException("Band staff profile not found");

            return staff;
        }



        public async Task<List<MyStudentDto>> GetMyStudentsAsync(string staffUserId, string? filterStatus = null)
        {
            var staff = await GetStaffMemberAsync(staffUserId);

            // Get all students this staff member has contacted or made offers to
            var contactedStudentIds = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id)
                .Select(cr => cr.StudentId)
                .Distinct()
                .ToListAsync();

            var offeredStudentIds = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id)
                .Select(o => o.StudentId)
                .Distinct()
                .ToListAsync();

            var allStudentIds = contactedStudentIds.Union(offeredStudentIds).Distinct().ToList();

            if (!allStudentIds.Any())
                return new List<MyStudentDto>();

            // Get students with all related data
            var students = await _unitOfWork.Students.GetQueryable()
                .Where(s => allStudentIds.Contains(s.Id) && !s.IsDeleted)
                .Include(s => s.Videos)
                .Include(s => s.StudentRatings)
                .ToListAsync();

            // Get contacts for these students
            var contacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && allStudentIds.Contains(cr.StudentId))
                .ToListAsync();

            // Get offers for these students
            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id && allStudentIds.Contains(o.StudentId))
                .ToListAsync();

            // Get ratings by this staff member
            var ratings = await _unitOfWork.StudentRatings.GetQueryable()
                .Where(r => r.BandStaffId == staff.Id && allStudentIds.Contains(r.StudentId))
                .ToListAsync();

            var result = students.Select(s =>
            {
                var contact = contacts.FirstOrDefault(c => c.StudentId == s.Id);
                var latestOffer = offers.Where(o => o.StudentId == s.Id).OrderByDescending(o => o.CreatedAt).FirstOrDefault();
                var myRating = ratings.FirstOrDefault(r => r.StudentId == s.Id);

                return new MyStudentDto
                {
                    StudentId = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = $"{s.FirstName} {s.LastName}",
                    ProfilePhotoUrl = s.ProfilePhotoUrl,
                    PrimaryInstrument = s.PrimaryInstrument ?? "",
                    State = s.State,
                    GraduationYear = s.GraduationYear,
                    GPA = s.GPA,

                    ContactedDate = contact?.RequestedDate,
                    ContactStatus = contact?.Status ?? "",
                    OfferSentDate = latestOffer?.CreatedAt,
                    OfferAmount = latestOffer?.ScholarshipAmount,
                    OfferStatus = latestOffer?.Status.ToString(),
                    MyRating = myRating?.Rating,
                    LastRatedDate = myRating?.CreatedAt,

                    VideoCount = s.Videos.Count,
                    AverageRating = s.StudentRatings.Any() ? s.StudentRatings.Average(r => r.Rating) : null,
                    TotalRatings = s.StudentRatings.Count,
                    LastActivityDate = s.LastActivityDate,

                    CanContact = contact == null && staff.CanContact,
                    CanMakeOffer = contact?.Status == "Approved" && staff.CanMakeOffers,
                    CanRate = staff.CanRateStudents
                };
            }).ToList();

            // Apply filter if specified
            if (!string.IsNullOrEmpty(filterStatus))
            {
                result = result.Where(s => s.ContactStatus.Equals(filterStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return result.OrderByDescending(s => s.ContactedDate ?? s.OfferSentDate ?? DateTime.MinValue).ToList();
        }

        public async Task<BandStaffPerformanceDto> GetMyPerformanceAsync(string staffUserId, DateTime startDate, DateTime endDate)
        {
            var staff = await GetStaffMemberAsync(staffUserId);

            // Get funnel data
            var contactedStudentIds = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && cr.RequestedDate >= startDate && cr.RequestedDate <= endDate)
                .Select(cr => cr.StudentId)
                .Distinct()
                .ToListAsync();

            var interestedStudentIds = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id &&
                            cr.RequestedDate >= startDate &&
                            cr.RequestedDate <= endDate &&
                            cr.Status == "Approved")
                .Select(cr => cr.StudentId)
                .Distinct()
                .ToListAsync();

            var offeredStudentIds = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Select(o => o.StudentId)
                .Distinct()
                .ToListAsync();

            var acceptedStudentIds = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id &&
                           o.CreatedAt >= startDate &&
                           o.CreatedAt <= endDate &&
                           o.Status == ScholarshipStatus.Accepted)
                .Select(o => o.StudentId)
                .Distinct()
                .ToListAsync();

            var studentsContacted = contactedStudentIds.Count;
            var studentsInterested = interestedStudentIds.Count;
            var offersExtended = offeredStudentIds.Count;
            var offersAccepted = acceptedStudentIds.Count;
            var studentsEnrolled = (int)(offersAccepted * 0.85); // Estimate

            // Get time series data
            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var contacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && cr.RequestedDate >= startDate && cr.RequestedDate <= endDate)
                .ToListAsync();

            var monthlyMetrics = offers
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new PerformanceTimeSeriesDto
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    OffersCreated = g.Count(),
                    OffersAccepted = g.Count(o => o.Status == ScholarshipStatus.Accepted),
                    ContactsMade = contacts.Count(c => c.RequestedDate.Year == g.Key.Year && c.RequestedDate.Month == g.Key.Month),
                    ResponsesReceived = contacts.Count(c => c.RequestedDate.Year == g.Key.Year && c.RequestedDate.Month == g.Key.Month && c.Status == "Approved")
                })
                .OrderBy(m => m.Date)
                .ToList();

            // Calculate team averages for comparison
            var teamOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == staff.BandId && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            var teamContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandId == staff.BandId && cr.RequestedDate >= startDate && cr.RequestedDate <= endDate)
                .ToListAsync();

            var teamAcceptanceRate = teamOffers.Any()
                ? (double)teamOffers.Count(o => o.Status == ScholarshipStatus.Accepted) / teamOffers.Count * 100
                : 0;

            var teamResponseRate = teamContacts.Any()
                ? (double)teamContacts.Count(c => c.Status == "Approved") / teamContacts.Count * 100
                : 0;

            var myAcceptanceRate = offers.Any()
                ? (double)offers.Count(o => o.Status == ScholarshipStatus.Accepted) / offers.Count * 100
                : 0;

            var myResponseRate = contacts.Any()
                ? (double)contacts.Count(c => c.Status == "Approved") / contacts.Count * 100
                : 0;

            return new BandStaffPerformanceDto
            {
                StudentsContacted = studentsContacted,
                StudentsInterested = studentsInterested,
                OffersExtended = offersExtended,
                OffersAccepted = offersAccepted,
                StudentsEnrolled = studentsEnrolled,

                ContactToInterestRate = studentsContacted > 0 ? (double)studentsInterested / studentsContacted * 100 : 0,
                InterestToOfferRate = studentsInterested > 0 ? (double)offersExtended / studentsInterested * 100 : 0,
                OfferToAcceptanceRate = offersExtended > 0 ? (double)offersAccepted / offersExtended * 100 : 0,
                OverallConversionRate = studentsContacted > 0 ? (double)offersAccepted / studentsContacted * 100 : 0,

                MonthlyMetrics = monthlyMetrics,

                MyAcceptanceRate = myAcceptanceRate,
                TeamAverageAcceptanceRate = teamAcceptanceRate,
                MyResponseRate = myResponseRate,
                TeamAverageResponseRate = teamResponseRate
            };
        }

        public async Task<List<MyActivityDto>> GetMyActivityAsync(string staffUserId, int limit = 20)
        {
            var staff = await GetStaffMemberAsync(staffUserId);
            var activities = new List<MyActivityDto>();

            // Get recent offers
            var recentOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Where(o => o.CreatedByStaffId == staff.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var offer in recentOffers)
            {
                var activityType = offer.Status switch
                {
                    ScholarshipStatus.Accepted => "OfferAccepted",
                    ScholarshipStatus.Declined => "OfferDeclined",
                    _ => "OfferCreated"
                };

                activities.Add(new MyActivityDto
                {
                    Id = offer.Id,
                    Timestamp = offer.UpdatedAt ?? offer.CreatedAt,
                    ActivityType = activityType,
                    Description = $"${offer.ScholarshipAmount:N0} scholarship to {offer.Student.FirstName} {offer.Student.LastName}",
                    StudentId = offer.StudentId,
                    StudentName = $"{offer.Student.FirstName} {offer.Student.LastName}",
                    Details = offer.OfferType,
                    Icon = "💰",
                    Color = activityType == "OfferAccepted" ? "green" : "blue"
                });
            }

            // Get recent contacts
            var recentContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student)
                .Where(cr => cr.BandStaffId == staff.Id)
                .OrderByDescending(cr => cr.RequestedDate)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var contact in recentContacts)
            {
                activities.Add(new MyActivityDto
                {
                    Id = contact.Id,
                    Timestamp = contact.ResponseDate ?? contact.RequestedDate,
                    ActivityType = "ContactMade",
                    Description = $"Contacted {contact.Student.FirstName} {contact.Student.LastName}",
                    StudentId = contact.StudentId,
                    StudentName = $"{contact.Student.FirstName} {contact.Student.LastName}",
                    Details = $"Status: {contact.Status}",
                    Icon = "📞",
                    Color = "purple"
                });
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }

        public async Task<List<MyPendingTaskDto>> GetMyPendingTasksAsync(string staffUserId)
        {
            var staff = await GetStaffMemberAsync(staffUserId);
            var tasks = new List<MyPendingTaskDto>();

            // Get pending contact requests (awaiting guardian approval)
            var pendingContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student)
                .Where(cr => cr.BandStaffId == staff.Id && cr.Status == "Pending")
                .ToListAsync();

            foreach (var contact in pendingContacts)
            {
                tasks.Add(new MyPendingTaskDto
                {
                    Id = contact.Id,
                    TaskType = "ResponseNeeded",
                    Title = "Awaiting Response",
                    Description = $"Contact request to {contact.Student.FirstName} {contact.Student.LastName} pending guardian approval",
                    DueDate = contact.RequestedDate.AddDays(7),
                    Priority = (DateTime.UtcNow - contact.RequestedDate).Days > 5 ? "High" : "Medium",
                    StudentId = contact.StudentId,
                    StudentName = $"{contact.Student.FirstName} {contact.Student.LastName}",
                    CanComplete = false
                });
            }

            // Get offers expiring soon
            var expiringOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Where(o => o.CreatedByStaffId == staff.Id &&
                           o.Status == ScholarshipStatus.Sent &&
                           o.ExpirationDate <= DateTime.UtcNow.AddDays(7))
                .ToListAsync();

            foreach (var offer in expiringOffers)
            {
                var daysUntilExpiration = (offer.ExpirationDate - DateTime.UtcNow).Days;
                tasks.Add(new MyPendingTaskDto
                {
                    Id = offer.Id,
                    TaskType = "OfferExpiring",
                    Title = "Offer Expiring Soon",
                    Description = $"${offer.ScholarshipAmount:N0} offer to {offer.Student.FirstName} {offer.Student.LastName} expires in {daysUntilExpiration} days",
                    DueDate = offer.ExpirationDate,
                    Priority = daysUntilExpiration <= 2 ? "High" : "Medium",
                    StudentId = offer.StudentId,
                    StudentName = $"{offer.Student.FirstName} {offer.Student.LastName}",
                    CanComplete = false
                });
            }

            // Get students who need follow-up (contacted >14 days ago, no offer)
            var needFollowUp = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student)
                .Where(cr => cr.BandStaffId == staff.Id &&
                            cr.Status == "Approved" &&
                            cr.RequestedDate < DateTime.UtcNow.AddDays(-14))
                .ToListAsync();

            var offeredStudentIds = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id)
                .Select(o => o.StudentId)
                .ToListAsync();

            foreach (var contact in needFollowUp.Where(c => !offeredStudentIds.Contains(c.StudentId)))
            {
                tasks.Add(new MyPendingTaskDto
                {
                    Id = contact.Id,
                    TaskType = "FollowUp",
                    Title = "Follow-up Needed",
                    Description = $"Follow up with {contact.Student.FirstName} {contact.Student.LastName} - no offer sent",
                    DueDate = contact.RequestedDate.AddDays(21),
                    Priority = "Low",
                    StudentId = contact.StudentId,
                    StudentName = $"{contact.Student.FirstName} {contact.Student.LastName}",
                    CanComplete = staff.CanMakeOffers
                });
            }

            return tasks.OrderBy(t => t.DueDate).ToList();
        }

        public async Task<QuickStatsDto> GetQuickStatsAsync(string staffUserId)
        {
            var staff = await GetStaffMemberAsync(staffUserId);

            var activeStudents = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && cr.Status == "Approved")
                .CountAsync();

            var pendingContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && cr.Status == "Pending")
                .CountAsync();

            var pendingOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id && o.Status == ScholarshipStatus.Sent)
                .CountAsync();

            var budgetUsed = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id &&
                           (o.Status == ScholarshipStatus.Accepted || o.Status == ScholarshipStatus.Sent))
                .SumAsync(o => o.ScholarshipAmount);

            var budgetRemaining = (staff.BudgetAllocation ?? 0) - budgetUsed;

            return new QuickStatsDto
            {
                ActiveStudents = activeStudents,
                PendingContacts = pendingContacts,
                PendingOffers = pendingOffers,
                BudgetRemaining = budgetRemaining
            };
        }

        public async Task<StaffStudentSearchDto> SearchStudentsAsync(string staffUserId, string? searchTerm, int page = 1, int pageSize = 20)
        {
            var staff = await GetStaffMemberAsync(staffUserId);

            var query = _unitOfWork.Students.GetQueryable()
                .Where(s => !s.IsDeleted);

            // Apply search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    (s.PrimaryInstrument != null && s.PrimaryInstrument.ToLower().Contains(term)) ||
                    (s.State != null && s.State.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var students = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.Videos)
                .Include(s => s.StudentRatings)
                .ToListAsync();

            // Get my interactions with these students
            var studentIds = students.Select(s => s.Id).ToList();

            var myContacts = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(cr => cr.BandStaffId == staff.Id && studentIds.Contains(cr.StudentId))
                .ToListAsync();

            var myOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.CreatedByStaffId == staff.Id && studentIds.Contains(o.StudentId))
                .ToListAsync();

            var myRatings = await _unitOfWork.StudentRatings.GetQueryable()
                .Where(r => r.BandStaffId == staff.Id && studentIds.Contains(r.StudentId))
                .ToListAsync();

            var results = students.Select(s =>
            {
                var contact = myContacts.FirstOrDefault(c => c.StudentId == s.Id);
                var latestOffer = myOffers.Where(o => o.StudentId == s.Id).OrderByDescending(o => o.CreatedAt).FirstOrDefault();
                var rating = myRatings.FirstOrDefault(r => r.StudentId == s.Id);

                return new MyStudentDto
                {
                    StudentId = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = $"{s.FirstName} {s.LastName}",
                    ProfilePhotoUrl = s.ProfilePhotoUrl,
                    PrimaryInstrument = s.PrimaryInstrument ?? "",
                    State = s.State,
                    GraduationYear = s.GraduationYear,
                    GPA = s.GPA,

                    ContactedDate = contact?.RequestedDate,
                    ContactStatus = contact?.Status ?? "",
                    OfferSentDate = latestOffer?.CreatedAt,
                    OfferAmount = latestOffer?.ScholarshipAmount,
                    OfferStatus = latestOffer?.Status.ToString(),
                    MyRating = rating?.Rating,

                    VideoCount = s.Videos.Count,
                    AverageRating = s.StudentRatings.Any() ? s.StudentRatings.Average(r => r.Rating) : null,
                    TotalRatings = s.StudentRatings.Count,
                    LastActivityDate = s.LastActivityDate,

                    CanContact = contact == null && staff.CanContact,
                    CanMakeOffer = contact?.Status == "Approved" && staff.CanMakeOffers,
                    CanRate = staff.CanRateStudents
                };
            }).ToList();

            return new StaffStudentSearchDto
            {
                Results = results,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}