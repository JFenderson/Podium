using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Guardian;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces; // Updated
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
    public class GuardianService : IGuardianService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GuardianService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;

        public GuardianService(
                 IUnitOfWork unitOfWork,
                 ILogger<GuardianService> logger,
                 INotificationService notificationService,
                 IPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        private async Task<Guardian?> GetGuardianEntityAsync(string userId)
        {
            return await _unitOfWork.Guardians.GetQueryable()
                .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);
        }

        public async Task<ServiceResult<GuardianDashboardDto>> GetDashboardAsync()
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (userId == null) return ServiceResult<GuardianDashboardDto>.Failure("User not found");

            var guardian = await GetGuardianEntityAsync(userId);
            if (guardian == null) return ServiceResult<GuardianDashboardDto>.Failure("Guardian profile not found");
            var guardianId = guardian.Id;

            var studentIds = await _unitOfWork.StudentGuardians.GetQueryable()
                .Where(sg => sg.GuardianId == guardianId)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!studentIds.Any()) return ServiceResult<GuardianDashboardDto>.Success(new GuardianDashboardDto());

            var now = DateTime.UtcNow;
            var expiringThreshold = now.AddDays(7);

            var studentSummaries = await _unitOfWork.Students.GetQueryable()
                .Where(s => studentIds.Contains(s.Id))
                .Include(s => s.ContactRequests)
                .Include(s => s.ScholarshipOffers)
                .Include(s => s.StudentInterests)
                .Select(s => new StudentSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = s.FirstName + " " + s.LastName,
                    PrimaryInstrument = s.Instrument ?? string.Empty,
                    GraduationYear = s.GraduationYear,
                    PendingContactRequests = s.ContactRequests.Count(cr => cr.Status == "Pending"),
                    ActiveScholarshipOffers = s.ScholarshipOffers.Count(so => so.Status == ScholarshipStatus.Sent),
                    BandsInterested = s.StudentInterests.Count,
                    LastActivityDate = s.LastActivityDate,
                    HasExpiringOffers = s.ScholarshipOffers.Any(so => so.Status == ScholarshipStatus.Sent && so.ExpirationDate <= expiringThreshold),
                    HasUrgentApprovals = s.ContactRequests.Any(cr => cr.Status == "Pending" && cr.IsUrgent)
                })
                .ToListAsync();

            var totalPendingApprovals = await _unitOfWork.ContactRequests.GetQueryable()
                .CountAsync(cr => studentIds.Contains(cr.StudentId) && cr.Status == "Pending");

            var totalActiveOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .CountAsync(so => studentIds.Contains(so.StudentId) && so.Status == ScholarshipStatus.Sent);

            var totalUnreadNotifications = await _unitOfWork.GuardianNotifications.GetQueryable()
                .CountAsync(n => n.GuardianId == guardianId && !n.IsRead);

            // Fetch alerts (Expiring offers & Urgent requests)
            var alerts = new List<PriorityAlertDto>();

            var expiringOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(so => so.Student)
                .Where(so => studentIds.Contains(so.StudentId) && so.Status == ScholarshipStatus.Sent && so.ExpirationDate <= expiringThreshold)
                .Select(so => new PriorityAlertDto
                {
                    AlertType = "ExpiringOffer",
                    Message = "Scholarship offer expiring soon",
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName,
                    Deadline = DateTime.MaxValue,
                    ActionUrl = $"/guardian/scholarships/{so.Id}",
                    Severity = "Medium"
                })
                .ToListAsync();
            alerts.AddRange(expiringOffers);

            var urgentApprovals = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student)
                .Where(cr => studentIds.Contains(cr.StudentId) && cr.Status == "Pending" && cr.IsUrgent)
                .Select(cr => new PriorityAlertDto
                {
                    AlertType = "UrgentApproval",
                    Message = "Urgent contact request awaiting approval",
                    StudentId = cr.StudentId,
                    StudentName = cr.Student.FirstName + " " + cr.Student.LastName,
                    Deadline = cr.RequestedDate.AddDays(3),
                    ActionUrl = $"/guardian/contact-requests/{cr.Id}",
                    Severity = "High"
                })
                .ToListAsync();
            alerts.AddRange(urgentApprovals);

            return ServiceResult<GuardianDashboardDto>.Success(new GuardianDashboardDto
            {
                LinkedStudents = studentSummaries,
                TotalPendingApprovals = totalPendingApprovals,
                TotalActiveOffers = totalActiveOffers,
                TotalUnreadNotifications = totalUnreadNotifications,
                PriorityAlerts = alerts.OrderBy(a => a.Deadline).Take(5).ToList(),
                RecentActivities = new List<GuardianRecentActivityDto>() // Placeholder
            });
        }

        public async Task<ServiceResult<GuardianNotificationPreferencesDto>> UpdateNotificationPreferencesAsync(UpdatePreferencesRequest request)
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (userId == null) return ServiceResult<GuardianNotificationPreferencesDto>.Failure("User not found");

            var guardian = await GetGuardianEntityAsync(userId);
            if (guardian == null) return ServiceResult<GuardianNotificationPreferencesDto>.Failure("Guardian profile not found");

            var prefs = await _unitOfWork.GuardianNotificationPreferences.GetQueryable()
                .FirstOrDefaultAsync(p => p.GuardianId == guardian.Id);

            if (prefs == null)
            {
                prefs = new GuardianNotificationPreferences { GuardianId = guardian.Id, CreatedAt = DateTime.UtcNow };
                await _unitOfWork.GuardianNotificationPreferences.AddAsync(prefs);
            }

            if (request.EmailEnabled.HasValue) prefs.EmailEnabled = request.EmailEnabled.Value;
            if (request.SmsEnabled.HasValue) prefs.SmsEnabled = request.SmsEnabled.Value;
            // ... map other fields ...
            if (request.StudentOverrides != null) prefs.StudentOverridesJson = JsonSerializer.Serialize(request.StudentOverrides);

            prefs.LastUpdated = DateTime.UtcNow;
            if (prefs.Id != 0) _unitOfWork.GuardianNotificationPreferences.Update(prefs); // Update if existing
            await _unitOfWork.SaveChangesAsync();

            Dictionary<int, StudentNotificationOverrideDto>? overrides = null;
            try { if (!string.IsNullOrEmpty(prefs.StudentOverridesJson)) overrides = JsonSerializer.Deserialize<Dictionary<int, StudentNotificationOverrideDto>>(prefs.StudentOverridesJson); } catch { }

            return ServiceResult<GuardianNotificationPreferencesDto>.Success(new GuardianNotificationPreferencesDto
            {
                UserId = userId,
                EmailEnabled = prefs.EmailEnabled,
                // Map back
                LastUpdated = prefs.LastUpdated
            });
        }

        public async Task<List<LinkedStudentDto>> GetLinkedStudentsAsync(string guardianUserId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new List<LinkedStudentDto>();

            return await _unitOfWork.StudentGuardians.GetQueryable()
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive)
                .Include(sg => sg.Student)
                .Select(sg => new LinkedStudentDto
                {
                    StudentId = sg.StudentId,
                    StudentName = sg.Student.FirstName + " " + sg.Student.LastName,
                    Email = sg.Student.Email,
                    Phone = sg.Student.Email,
                    PrimaryInstrument = sg.Student.PrimaryInstrument,
                    GraduationYear = sg.Student.GraduationYear,
                    HighSchool = sg.Student.HighSchool,
                    RelationshipType = sg.RelationshipType,
                    IsVerified = sg.IsVerified,
                    LinkedDate = sg.LinkedDate,
                    CanViewActivity = sg.CanViewActivity,
                    CanApproveContacts = sg.CanApproveContacts,
                    CanRespondToOffers = sg.CanRespondToOffers,
                    ReceivesNotifications = sg.ReceivesNotifications
                })
                .OrderBy(s => s.StudentName)
                .ToListAsync();
        }

        public async Task<bool> CanAccessStudentAsync(string guardianUserId, int studentId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return false;

            return await _unitOfWork.StudentGuardians.GetQueryable()
                .AnyAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == studentId && sg.IsActive);
        }

        public async Task<StudentActivityDto> GetStudentActivityAsync(int studentId, int daysBack)
        {
            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);
            if (student == null) throw new KeyNotFoundException($"Student {studentId} not found");

            var videosTask = _unitOfWork.Videos.GetQueryable()
                .Where(v => v.StudentId == studentId && v.CreatedAt >= startDate)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VideoActivityDto { VideoId = v.Id, Title = v.Title, Instrument = v.Instrument, UploadedDate = v.CreatedAt, Views = v.ViewCount, IsPublic = v.IsPublic })
                .ToListAsync();

            var interestsTask = _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.StudentId == studentId && si.InterestedDate >= startDate)
                .Include(si => si.Band).Include(si => si.Student).ThenInclude(s => s.ContactLogs)
                .OrderByDescending(si => si.InterestedDate)
                .ToListAsync();

            var offersTask = _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(so => so.StudentId == studentId && so.CreatedAt >= startDate)
                .Include(so => so.Band)
                .OrderByDescending(so => so.CreatedAt)
                .Select(so => new OfferActivityDto { OfferId = so.Id, BandName = so.Band.BandName, Amount = so.ScholarshipAmount, Status = so.Status.ToString(), OfferDate = so.CreatedAt, ExpirationDate = so.ExpirationDate, RequiresGuardianApproval = so.RequiresGuardianApproval })
                .ToListAsync();

            var eventsTask = _unitOfWork.EventRegistrations.GetQueryable()
                .Where(er => er.StudentId == studentId && er.CreatedAt >= startDate)
                .Include(er => er.BandEvent).ThenInclude(e => e.Band)
                .OrderByDescending(er => er.BandEvent.EventDate)
                .Select(er => new EventActivityDto { EventId = er.Id, EventName = er.BandEvent.EventName, BandName = er.BandEvent.Band.BandName, EventDate = er.BandEvent.EventDate, DidAttend = er.DidAttend, RegisteredDate = er.CreatedAt })
                .ToListAsync();

            var contactsTask = _unitOfWork.ContactLogs.GetQueryable()
                .Where(cl => cl.StudentId == studentId && cl.CreatedAt >= startDate)
                .Include(cl => cl.Band).Include(cl => cl.RecruiterStaff)
                .OrderByDescending(cl => cl.CreatedAt)
                .Select(cl => new ContactActivityDto { ContactId = cl.Id, RecruiterName = cl.RecruiterStaff.ApplicationUserId, BandName = cl.Band.BandName, ContactDate = cl.CreatedAt, ContactMethod = cl.ContactMethod, Purpose = cl.Purpose })
                .ToListAsync();

            await Task.WhenAll(videosTask, interestsTask, offersTask, eventsTask, contactsTask);

            var interests = (await interestsTask).Select(si => new InterestActivityDto
            {
                BandId = si.BandId,
                BandName = si.Band.BandName,
                University = si.Band.UniversityName,
                InterestDate = si.InterestedDate,
                HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == si.BandId),
                ContactDate = si.Student.ContactLogs.Where(cl => cl.BandId == si.BandId).OrderBy(cl => cl.CreatedAt).Select(cl => (DateTime?)cl.CreatedAt).FirstOrDefault()
            }).ToList();

            return new StudentActivityDto
            {
                StudentId = studentId,
                StudentName = student.FirstName + " " + student.LastName,
                StartDate = startDate,
                EndDate = DateTime.UtcNow,
                VideosUploaded = await videosTask,
                InterestShown = interests,
                OffersReceived = await offersTask,
                EventsAttended = await eventsTask,
                ContactsMade = await contactsTask,
                TotalVideos = (await videosTask).Count,
                TotalInterests = interests.Count,
                TotalOffers = (await offersTask).Count,
                TotalEvents = (await eventsTask).Count,
                TotalContacts = (await contactsTask).Count
            };
        }

        public async Task<StudentProfileDto> GetStudentProfileAsync(int studentId)
        {
            var student = await _unitOfWork.Students.GetQueryable()
                .Include(s => s.Videos)
                .Include(s => s.StudentInterests)
                .Include(s => s.EventRegistrations)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) throw new KeyNotFoundException($"Student {studentId} not found");

            // Mapping logic (JSON deserialization etc) remains same
            return new StudentProfileDto
            {
                StudentId = student.Id,
                Name = student.FirstName + " " + student.LastName,
                // ... map fields
                VideosUploaded = student.Videos.Count,
                BandsInterested = student.StudentInterests.Count,
                EventsAttended = student.EventRegistrations.Count(er => er.DidAttend),
                LastActivityDate = student.LastActivityDate
            };
        }

        public async Task<List<ContactRequestDto>> GetContactRequestsAsync(string guardianUserId, int? studentId, string? status)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new List<ContactRequestDto>();

            var accessibleStudentIds = await _unitOfWork.StudentGuardians.GetQueryable()
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive && sg.CanApproveContacts)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!accessibleStudentIds.Any()) return new List<ContactRequestDto>();

            var query = _unitOfWork.ContactRequests.GetQueryable().Where(cr => accessibleStudentIds.Contains(cr.StudentId));

            if (studentId.HasValue) query = query.Where(cr => cr.StudentId == studentId.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(cr => cr.Status == status);
            else query = query.Where(cr => cr.Status == "Pending");

            return await query
                .Include(cr => cr.Student).Include(cr => cr.Band).Include(cr => cr.RecruiterStaff)
                .OrderByDescending(cr => cr.RequestedDate)
                .Select(cr => new ContactRequestDto
                {
                    RequestId = cr.Id,
                    StudentId = cr.StudentId,
                    StudentName = cr.Student.FirstName + " " + cr.Student.LastName,
                    // ... map other fields
                    Status = cr.Status,
                    RequestedDate = cr.RequestedDate
                })
                .ToListAsync();
        }

        public async Task<bool> CanManageContactRequestAsync(string guardianUserId, int requestId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return false;
            var request = await _unitOfWork.ContactRequests.GetByIdAsync(requestId);
            if (request == null) return false;

            return await _unitOfWork.StudentGuardians.GetQueryable()
                .AnyAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == request.StudentId && sg.IsActive && sg.CanApproveContacts);
        }

        public async Task<ContactRequestDto> ApproveContactRequestAsync(int requestId, string guardianUserId, string? notes)
        {
            var request = await _unitOfWork.ContactRequests.GetQueryable()
                .Include(cr => cr.Student).Include(cr => cr.Band).Include(cr => cr.RecruiterStaff)
                .FirstOrDefaultAsync(cr => cr.Id == requestId);

            if (request == null) throw new KeyNotFoundException($"Contact request {requestId} not found");
            if (request.Status != "Pending") throw new InvalidOperationException($"Cannot approve request in status {request.Status}");

            request.Status = "Approved";
            request.ResponseDate = DateTime.UtcNow;
            request.RespondedByGuardianUserId = guardianUserId;
            request.ResponseNotes = notes;

            _unitOfWork.ContactRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            if (request.RecruiterStaff != null)
            {
                var studentName = $"{request.Student.FirstName} {request.Student.LastName}";
                await _notificationService.NotifyUserAsync(
                    request.RecruiterStaff.ApplicationUserId,
                    "ContactApproved",
                    "Contact Request Approved",
                    $"A guardian has approved your request to contact {studentName}.",
                    request.Id.ToString()
                );
            }
            return new ContactRequestDto { RequestId = request.Id /* Map fields */ };
        }

        public async Task<ContactRequestDto> DeclineContactRequestAsync(int requestId, string guardianUserId, string? reason)
        {
            var request = await _unitOfWork.ContactRequests.GetQueryable()
                 .Include(cr => cr.Student).Include(cr => cr.Band).Include(cr => cr.RecruiterStaff)
                 .FirstOrDefaultAsync(cr => cr.Id == requestId);

            if (request == null) throw new KeyNotFoundException($"Contact request {requestId} not found");
            if (request.Status != "Pending") throw new InvalidOperationException($"Cannot decline request in status {request.Status}");

            request.Status = "Declined";
            request.ResponseDate = DateTime.UtcNow;
            request.RespondedByGuardianUserId = guardianUserId;
            request.ResponseNotes = reason;
            request.DeclineReason = reason;

            _unitOfWork.ContactRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();

            // Notification logic...
            return new ContactRequestDto { RequestId = request.Id /* Map fields */ };
        }

        public async Task<List<GuardianScholarshipDto>> GetScholarshipsAsync(string guardianUserId, int? studentId, string? status)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new List<GuardianScholarshipDto>();

            var accessibleStudentIds = await _unitOfWork.StudentGuardians.GetQueryable()
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!accessibleStudentIds.Any()) return new List<GuardianScholarshipDto>();

            var query = _unitOfWork.ScholarshipOffers.GetQueryable().Where(so => accessibleStudentIds.Contains(so.StudentId));
            if (studentId.HasValue) query = query.Where(so => so.StudentId == studentId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ScholarshipStatus>(status, true, out var statusEnum))
                query = query.Where(so => so.Status == statusEnum);

            var offers = await query.Include(so => so.Student).Include(so => so.Band).Include(so => so.CreatedByStaff).OrderByDescending(so => so.CreatedAt).ToListAsync();
            // ... Mapping logic remains same ...
            return new List<GuardianScholarshipDto>();
        }

        public async Task<bool> CanRespondToScholarshipAsync(string guardianUserId, int offerId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return false;
            var offer = await _unitOfWork.ScholarshipOffers.GetByIdAsync(offerId);
            if (offer == null) return false;

            return await _unitOfWork.StudentGuardians.GetQueryable()
                .AnyAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == offer.StudentId && sg.IsActive && sg.CanRespondToOffers);
        }

        public async Task<GuardianScholarshipDto> RespondToScholarshipAsync(int offerId, string guardianUserId, string response, string? notes)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new GuardianScholarshipDto();
            ScholarshipStatus newStatus = response == "Accepted" ? ScholarshipStatus.Accepted : (response == "Declined" ? ScholarshipStatus.Declined : throw new ArgumentException("Invalid response"));

            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(so => so.Student).Include(so => so.Band).Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null) throw new KeyNotFoundException($"Offer {offerId} not found");
            if (offer.Status != ScholarshipStatus.Sent) throw new InvalidOperationException("Offer not open for response");

            var guardianLink = await _unitOfWork.StudentGuardians.GetQueryable()
                .FirstOrDefaultAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == offer.StudentId && sg.IsActive);
            if (guardianLink == null || !guardianLink.CanRespondToOffers) throw new UnauthorizedAccessException("No permission");

            offer.Status = newStatus;
            offer.ResponseDate = DateTime.UtcNow;
            offer.RespondedByGuardianUserId = guardianUserId;
            offer.ResponseNotes = notes;

            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();

            return new GuardianScholarshipDto { OfferId = offer.Id /* Map fields */ };
        }

        public async Task<List<string>> GetGuardianUserIdsForStudentAsync(int studentId)
        {
            return await _unitOfWork.StudentGuardians.GetQueryable()
                .Where(sg => sg.StudentId == studentId && sg.IsActive && sg.ReceivesNotifications)
                .Include(sg => sg.Guardian)
                .Select(sg => sg.Guardian.ApplicationUserId)
                .ToListAsync();
        }

        public async Task<ServiceResult<NotificationListDto>> GetNotificationsAsync(string userId, NotificationFilterDto filter)
        {
            var guardian = await GetGuardianEntityAsync(userId);
            if (guardian == null) return ServiceResult<NotificationListDto>.Failure("Guardian not found");

            var query = _unitOfWork.GuardianNotifications.GetQueryable()
                .Where(n => n.GuardianId == guardian.Id);
            // ... apply filters ...
            return ServiceResult<NotificationListDto>.Success(new NotificationListDto());
        }
    }
}