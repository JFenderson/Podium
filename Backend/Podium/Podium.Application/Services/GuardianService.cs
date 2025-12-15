
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Guardian;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace Podium.Application.Services
{
    public class GuardianService : IGuardianService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GuardianService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;

        public GuardianService(
                 ApplicationDbContext context,
                 ILogger<GuardianService> logger,
                 INotificationService notificationService,
                 IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Helper method to get Guardian entity ID from ApplicationUser ID
        /// </summary>
        private async Task<Guardian?> GetGuardianEntityAsync(string userId)
        {
            return await _context.Guardians
                .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);
        }

        public async Task<ServiceResult<GuardianDashboardDto>> GetDashboardAsync()
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (userId == null) return ServiceResult<GuardianDashboardDto>.Failure("User not found");

            // 1. Resolve String ID (User) -> Int ID (Guardian)
            var guardian = await _context.Guardians
                .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);

            if (guardian == null)
                return ServiceResult<GuardianDashboardDto>.Failure("Guardian profile not found");

            var guardianId = guardian.Id; // Use the Int ID from BaseEntity

            // 2. Get Linked Student IDs
            var studentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianId == guardianId)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!studentIds.Any())
                return ServiceResult<GuardianDashboardDto>.Success(new GuardianDashboardDto());

            var now = DateTime.UtcNow;
            var expiringThreshold = now.AddDays(7);

            // 3. Build Student Summaries
            var studentSummaries = await _context.Students
                .Where(s => studentIds.Contains(s.Id))
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
                    HasExpiringOffers = s.ScholarshipOffers.Any(so =>
                        so.Status == ScholarshipStatus.Sent && so.ExpirationDate <= expiringThreshold),
                    HasUrgentApprovals = s.ContactRequests.Any(cr => cr.Status == "Pending" && cr.IsUrgent)
                })
                .ToListAsync();

            // 4. Aggregate Counts
            var totalPendingApprovals = await _context.ContactRequests
                .CountAsync(cr => studentIds.Contains(cr.StudentId) && cr.Status == "Pending");

            var totalActiveOffers = await _context.Offers
                .CountAsync(so => studentIds.Contains(so.StudentId) && so.Status == ScholarshipStatus.Sent);

            var totalUnreadNotifications = await _context.GuardianNotifications
                .CountAsync(n => n.Id == guardianId && !n.IsRead);

            // 5. Get Priority Alerts
            var alerts = new List<PriorityAlertDto>();

            // Alert A: Expiring Offers
            var expiringOffers = await _context.Offers
                .Include(so => so.Student)
                .Where(so => studentIds.Contains(so.StudentId) &&
                             so.Status == ScholarshipStatus.Sent &&
                             so.ExpirationDate <= expiringThreshold)
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

            // Alert B: Urgent Approvals
            var urgentApprovals = await _context.ContactRequests
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

            // 6. Recent Activity (Placeholder logic - you can implement this later)
            var recentActivity = new List<GuardianRecentActivityDto>();

            return ServiceResult<GuardianDashboardDto>.Success(new GuardianDashboardDto
            {
                LinkedStudents = studentSummaries,
                TotalPendingApprovals = totalPendingApprovals,
                TotalActiveOffers = totalActiveOffers,
                TotalUnreadNotifications = totalUnreadNotifications,
                PriorityAlerts = alerts.OrderBy(a => a.Deadline).Take(5).ToList(),
                RecentActivities = recentActivity
            });
        }

        public async Task<ServiceResult<GuardianNotificationPreferencesDto>> UpdateNotificationPreferencesAsync(UpdatePreferencesRequest request)
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (userId == null) return ServiceResult<GuardianNotificationPreferencesDto>.Failure("User not found");

            // 1. Resolve String ID -> Int ID
            var guardian = await _context.Guardians.FirstOrDefaultAsync(g => g.ApplicationUserId == userId);
            if (guardian == null) return ServiceResult<GuardianNotificationPreferencesDto>.Failure("Guardian profile not found");

            // 2. Find Preferences by GuardianId (Int)
            var prefs = await _context.GuardianNotificationPreferences
                .FirstOrDefaultAsync(p => p.GuardianId == guardian.Id);

            if (prefs == null)
            {
                prefs = new GuardianNotificationPreferences
                {
                    GuardianId = guardian.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GuardianNotificationPreferences.Add(prefs);
            }

            // 3. Update Fields
            if (request.EmailEnabled.HasValue) prefs.EmailEnabled = request.EmailEnabled.Value;
            if (request.SmsEnabled.HasValue) prefs.SmsEnabled = request.SmsEnabled.Value;
            if (request.InAppEnabled.HasValue) prefs.InAppEnabled = request.InAppEnabled.Value;
            if (request.NotifyOnNewOffer.HasValue) prefs.NotifyOnNewOffer = request.NotifyOnNewOffer.Value;
            if (request.NotifyOnContactRequest.HasValue) prefs.NotifyOnContactRequest = request.NotifyOnContactRequest.Value;
            if (request.NotifyOnOfferExpiring.HasValue) prefs.NotifyOnOfferExpiring = request.NotifyOnOfferExpiring.Value;
            if (request.OfferExpiringDaysThreshold.HasValue) prefs.OfferExpiringDaysThreshold = request.OfferExpiringDaysThreshold.Value;
            if (request.NotifyOnVideoUpload.HasValue) prefs.NotifyOnVideoUpload = request.NotifyOnVideoUpload.Value;
            if (request.NotifyOnInterestShown.HasValue) prefs.NotifyOnInterestShown = request.NotifyOnInterestShown.Value;
            if (request.NotifyOnEventRegistration.HasValue) prefs.NotifyOnEventRegistration = request.NotifyOnEventRegistration.Value;
            if (request.DigestFrequency != null) prefs.DigestFrequency = request.DigestFrequency;
            if (request.QuietHoursStart.HasValue) prefs.QuietHoursStart = request.QuietHoursStart;
            if (request.QuietHoursEnd.HasValue) prefs.QuietHoursEnd = request.QuietHoursEnd;

            if (request.StudentOverrides != null)
                prefs.StudentOverridesJson = JsonSerializer.Serialize(request.StudentOverrides);

            // 4. Map Response
            // Handle JSON deserialization safely
            Dictionary<int, StudentNotificationOverrideDto>? overrides = null;
            try
            {
                if (!string.IsNullOrEmpty(prefs.StudentOverridesJson))
                    overrides = JsonSerializer.Deserialize<Dictionary<int, StudentNotificationOverrideDto>>(prefs.StudentOverridesJson);
            }
            catch { }

            prefs.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<GuardianNotificationPreferencesDto>.Success(new GuardianNotificationPreferencesDto
            {
                UserId = userId,
                EmailEnabled = prefs.EmailEnabled,
                SmsEnabled = prefs.SmsEnabled,
                InAppEnabled = prefs.InAppEnabled,
                NotifyOnNewOffer = prefs.NotifyOnNewOffer,
                NotifyOnContactRequest = prefs.NotifyOnContactRequest,
                NotifyOnOfferExpiring = prefs.NotifyOnOfferExpiring,
                OfferExpiringDaysThreshold = prefs.OfferExpiringDaysThreshold,
                NotifyOnVideoUpload = prefs.NotifyOnVideoUpload,
                NotifyOnInterestShown = prefs.NotifyOnInterestShown,
                NotifyOnEventRegistration = prefs.NotifyOnEventRegistration,
                DigestFrequency = prefs.DigestFrequency,
                QuietHoursStart = prefs.QuietHoursStart,
                QuietHoursEnd = prefs.QuietHoursEnd,
                StudentOverrides = overrides,
                LastUpdated = prefs.LastUpdated
            });
        }

        public async Task<List<LinkedStudentDto>> GetLinkedStudentsAsync(string guardianUserId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null)
                return new List<LinkedStudentDto>();

            return await _context.StudentGuardians
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive)
                .Include(sg => sg.Student)
                .Select(sg => new LinkedStudentDto
                {
                    StudentId = sg.StudentId,
                    StudentName = sg.Student.FirstName + " " + sg.Student.LastName,
                    Email = sg.Student.Email,
                    Phone = sg.Student.Email, // Student entity doesn't have PhoneNumber
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
            if (guardianId == null)
                return false;

            return await _context.StudentGuardians
                .AnyAsync(sg =>
                    sg.GuardianId == guardianId.Id &&
                    sg.StudentId == studentId &&
                    sg.IsActive);
        }

        public async Task<StudentActivityDto> GetStudentActivityAsync(int studentId, int daysBack)
        {
            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            var student = await _context.Students.FindAsync(studentId);

            if (student == null)
                throw new KeyNotFoundException($"Student {studentId} not found");

            // --- Videos ---
            var videosTask = _context.Videos
                .Where(v => v.StudentId == studentId && v.CreatedAt >= startDate)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VideoActivityDto
                {
                    VideoId = v.Id,
                    Title = v.Title,
                    Instrument = v.Instrument,
                    UploadedDate = v.CreatedAt,
                    Views = v.ViewCount,
                    IsPublic = v.IsPublic
                })
                .ToListAsync();

            // --- Interests ---
            var interestsTask = _context.StudentInterests
                .Where(si => si.StudentId == studentId && si.InterestedDate >= startDate)
                .Include(si => si.Band)
                .Include(si => si.Student).ThenInclude(s => s.ContactLogs)
                .OrderByDescending(si => si.InterestedDate)
                .ToListAsync();

            // --- Offers (FIXED ENUM) ---
            var offersTask = _context.Offers
                .Where(so => so.StudentId == studentId && so.CreatedAt >= startDate)
                .Include(so => so.Band)
                .OrderByDescending(so => so.CreatedAt)
                .Select(so => new OfferActivityDto
                {
                    OfferId = so.Id,
                    BandName = so.Band.BandName,
                    Amount = so.ScholarshipAmount,
                    // Fix: Convert Enum to String for DTO
                    Status = so.Status.ToString(),
                    OfferDate = so.CreatedAt,
                    ExpirationDate = so.ExpirationDate,
                    RequiresGuardianApproval = so.RequiresGuardianApproval
                })
                .ToListAsync();

            // --- Events ---
            var eventsTask = _context.EventRegistrations
                .Where(er => er.StudentId == studentId && er.CreatedAt >= startDate)
                .Include(er => er.BandEvent).ThenInclude(e => e.Band)
                .OrderByDescending(er => er.BandEvent.EventDate)
                .Select(er => new EventActivityDto
                {
                    EventId = er.Id,
                    EventName = er.BandEvent.EventName,
                    BandName = er.BandEvent.Band.BandName,
                    EventDate = er.BandEvent.EventDate,
                    DidAttend = er.DidAttend,
                    RegisteredDate = er.CreatedAt
                })
                .ToListAsync();

            // --- Contacts ---
            var contactsTask = _context.ContactLogs
                .Where(cl => cl.StudentId == studentId && cl.CreatedAt >= startDate)
                .Include(cl => cl.Band)
                .Include(cl => cl.RecruiterStaff)
                .OrderByDescending(cl => cl.CreatedAt)
                .Select(cl => new ContactActivityDto
                {
                    ContactId = cl.Id,
                    RecruiterName = cl.RecruiterStaff.ApplicationUserId,
                    BandName = cl.Band.BandName,
                    ContactDate = cl.CreatedAt,
                    ContactMethod = cl.ContactMethod,
                    Purpose = cl.Purpose
                })
                .ToListAsync();

            await Task.WhenAll(videosTask, interestsTask, offersTask, eventsTask, contactsTask);

            // In-Memory Mapping for Interests
            var interests = (await interestsTask).Select(si => new InterestActivityDto
            {
                BandId = si.BandId,
                BandName = si.Band.BandName,
                University = si.Band.UniversityName,
                InterestDate = si.InterestedDate,
                HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == si.BandId),
                ContactDate = si.Student.ContactLogs
                    .Where(cl => cl.BandId == si.BandId)
                    .OrderBy(cl => cl.CreatedAt)
                    .Select(cl => (DateTime?)cl.CreatedAt)
                    .FirstOrDefault()
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
            var student = await _context.Students
                .Include(s => s.Videos)
                .Include(s => s.StudentInterests)
                .Include(s => s.EventRegistrations)
                .FirstOrDefaultAsync(s => s.Id == studentId); // Changed from Id

            if (student == null)
                throw new KeyNotFoundException($"Student {studentId} not found");

            var secondaryInstruments = new List<string>();
            if (!string.IsNullOrEmpty(student.SecondaryInstruments))
            {
                try
                {
                    secondaryInstruments = JsonSerializer.Deserialize<List<string>>(student.SecondaryInstruments) ?? new List<string>();
                }
                catch
                {
                    secondaryInstruments = student.SecondaryInstruments.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            var achievements = new List<string>();
            if (!string.IsNullOrEmpty(student.Achievements))
            {
                try
                {
                    achievements = JsonSerializer.Deserialize<List<string>>(student.Achievements) ?? new List<string>();
                }
                catch
                {
                    achievements = student.Achievements.Split(',').Select(s => s.Trim()).ToList();
                }
            }

            return new StudentProfileDto
            {
                StudentId = student.Id, // Changed from Id
                Name = student.FirstName + " " + student.LastName,
                Email = student.Email,
                Phone = student.Email, // Student doesn't have PhoneNumber
                Bio = student.Bio,
                HighSchool = student.HighSchool,
                GraduationYear = student.GraduationYear,
                GPA = student.GPA,
                IntendedMajor = student.IntendedMajor,
                PrimaryInstrument = student.PrimaryInstrument,
                SecondaryInstruments = secondaryInstruments,
                SkillLevel = student.SkillLevel,
                YearsExperience = student.YearsExperience,
                Achievements = achievements,
                VideosUploaded = student.Videos.Count,
                BandsInterested = student.StudentInterests.Count,
                EventsAttended = student.EventRegistrations.Count(er => er.DidAttend),
                LastActivityDate = student.LastActivityDate
            };
        }

        public async Task<List<ContactRequestDto>> GetContactRequestsAsync(string guardianUserId, int? studentId, string? status)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null)
                return new List<ContactRequestDto>();

            var accessibleStudentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive && sg.CanApproveContacts)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!accessibleStudentIds.Any())
                return new List<ContactRequestDto>();

            var query = _context.ContactRequests
                .Where(cr => accessibleStudentIds.Contains(cr.StudentId));

            if (studentId.HasValue)
                query = query.Where(cr => cr.StudentId == studentId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(cr => cr.Status == status);
            else
                query = query.Where(cr => cr.Status == "Pending"); // Default to pending only

            return await query
                .Include(cr => cr.Student)
                .Include(cr => cr.Band)
                .Include(cr => cr.RecruiterStaff)
                .OrderByDescending(cr => cr.RequestedDate)
                .Select(cr => new ContactRequestDto
                {
                    RequestId = cr.Id, // Changed from Id
                    StudentId = cr.StudentId,
                    StudentName = cr.Student.FirstName + " " + cr.Student.LastName,
                    BandId = cr.BandId,
                    BandName = cr.Band.BandName,
                    University = cr.Band.UniversityName,
                    RecruiterName = cr.RecruiterStaff.ApplicationUserId,
                    RecruiterTitle = cr.RecruiterStaff.Role,
                    Purpose = cr.Purpose,
                    PreferredContactMethod = cr.PreferredContactMethod,
                    RequestedDate = cr.RequestedDate,
                    Status = cr.Status,
                    ResponseDate = cr.ResponseDate,
                    ResponseNotes = cr.ResponseNotes,
                    IsUrgent = cr.IsUrgent
                })
                .ToListAsync();
        }

        public async Task<bool> CanManageContactRequestAsync(string guardianUserId, int requestId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null)
                return false;

            var request = await _context.ContactRequests
                .Include(cr => cr.Student)
                .FirstOrDefaultAsync(cr => cr.Id == requestId); // Changed from Id

            if (request == null)
                return false;

            return await _context.StudentGuardians.AnyAsync(sg =>
                sg.GuardianId == guardianId.Id &&
                sg.StudentId == request.StudentId &&
                sg.IsActive &&
                sg.CanApproveContacts);
        }

        // =========================================================================
        // SCENARIO 5: Contact request approved → notify recruiter(bandStaff)
        // =========================================================================
        public async Task<ContactRequestDto> ApproveContactRequestAsync(int requestId, string guardianUserId, string? notes)
        {
            var request = await _context.ContactRequests
                .Include(cr => cr.Student)
                .Include(cr => cr.Band)
                .Include(cr => cr.RecruiterStaff) // Ensure RecruiterStaff is included to get UserId
                .FirstOrDefaultAsync(cr => cr.Id == requestId);

            if (request == null)
                throw new KeyNotFoundException($"Contact request {requestId} not found");

            if (request.Status != "Pending")
                throw new InvalidOperationException($"Cannot approve request in status {request.Status}");

            request.Status = "Approved";
            request.ResponseDate = DateTime.UtcNow;
            request.RespondedByGuardianUserId = guardianUserId;
            request.ResponseNotes = notes;

            await _context.SaveChangesAsync();

            // NEW: Send Notification to Recruiter
            if (request.RecruiterStaff != null)
            {
                var studentName = $"{request.Student.FirstName} {request.Student.LastName}";

                await _notificationService.NotifyUserAsync(
                    request.RecruiterStaff.ApplicationUserId, // Target the recruiter
                    "ContactApproved",
                    "Contact Request Approved",
                    $"A guardian has approved your request to contact {studentName}.",
                    request.Id.ToString()
                );
            }

            return new ContactRequestDto
            {
                RequestId = request.Id,
                StudentId = request.StudentId,
                StudentName = request.Student.FirstName + " " + request.Student.LastName,
                BandId = request.BandId,
                BandName = request.Band.BandName,
                University = request.Band.UniversityName,
                RecruiterName = request.RecruiterStaff?.ApplicationUserId ?? "Unknown",
                RecruiterTitle = request.RecruiterStaff?.Role ?? "Staff",
                Purpose = request.Purpose,
                PreferredContactMethod = request.PreferredContactMethod,
                RequestedDate = request.RequestedDate,
                Status = request.Status,
                ResponseDate = request.ResponseDate,
                ResponseNotes = request.ResponseNotes,
                IsUrgent = request.IsUrgent
            };
        }

        public async Task<ContactRequestDto> DeclineContactRequestAsync(int requestId, string guardianUserId, string? reason)
        {
            var request = await _context.ContactRequests
                .Include(cr => cr.Student)
                .Include(cr => cr.Band)
                .Include(cr => cr.RecruiterStaff)
                .FirstOrDefaultAsync(cr => cr.Id == requestId);

            if (request == null)
                throw new KeyNotFoundException($"Contact request {requestId} not found");

            if (request.Status != "Pending")
                throw new InvalidOperationException($"Cannot decline request in status {request.Status}");

            request.Status = "Declined";
            request.ResponseDate = DateTime.UtcNow;
            request.RespondedByGuardianUserId = guardianUserId;
            request.ResponseNotes = reason;
            request.DeclineReason = reason;

            await _context.SaveChangesAsync();

            // OPTIONAL: Notify recruiter of decline (improves UX)
            if (request.RecruiterStaff != null)
            {
                var studentName = $"{request.Student.FirstName} {request.Student.LastName}";
                await _notificationService.NotifyUserAsync(
                    request.RecruiterStaff.ApplicationUserId,
                    "ContactDeclined",
                    "Contact Request Declined",
                    $"The guardian declined your request to contact {studentName}.",
                    request.Id.ToString()
                );
            }

            return new ContactRequestDto
            {
                RequestId = request.Id,
                StudentId = request.StudentId,
                StudentName = request.Student.FirstName + " " + request.Student.LastName,
                BandId = request.BandId,
                BandName = request.Band.BandName,
                University = request.Band.UniversityName,
                RecruiterName = request.RecruiterStaff?.ApplicationUserId ?? "Unknown",
                RecruiterTitle = request.RecruiterStaff?.Role ?? "Staff",
                Purpose = request.Purpose,
                PreferredContactMethod = request.PreferredContactMethod,
                RequestedDate = request.RequestedDate,
                Status = request.Status,
                ResponseDate = request.ResponseDate,
                ResponseNotes = request.ResponseNotes,
                IsUrgent = request.IsUrgent
            };
        }

        /// <summary>
        /// Get scholarship offers for linked students.
        /// AUTHORIZATION: Only returns offers for students guardian has access to.
        /// </summary>
        public async Task<List<GuardianScholarshipDto>> GetScholarshipsAsync(string guardianUserId, int? studentId, string? status)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new List<GuardianScholarshipDto>();

            var accessibleStudentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianId == guardianId.Id && sg.IsActive)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!accessibleStudentIds.Any()) return new List<GuardianScholarshipDto>();

            var query = _context.Offers.Where(so => accessibleStudentIds.Contains(so.StudentId));

            if (studentId.HasValue)
                query = query.Where(so => so.StudentId == studentId.Value);

            // FIX: Parse String input to Enum for Query
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ScholarshipStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(so => so.Status == statusEnum);
                }
            }

            var now = DateTime.UtcNow;

            // Materialize first because of complex processing
            var offers = await query
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .OrderByDescending(so => so.CreatedAt)
                .ToListAsync();

            var result = new List<GuardianScholarshipDto>();
            foreach (var offer in offers)
            {
                var guardianLink = await _context.StudentGuardians
                    .FirstOrDefaultAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == offer.StudentId && sg.IsActive);

                var canRespond = guardianLink?.CanRespondToOffers ?? false;

                result.Add(new GuardianScholarshipDto
                {
                    OfferId = offer.Id,
                    StudentId = offer.StudentId,
                    StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                    BandName = offer.Band.BandName,
                    University = offer.Band.UniversityName,
                    Amount = offer.ScholarshipAmount,
                    OfferType = offer.OfferType,
                    // Fix: Convert Enum to String
                    Status = offer.Status.ToString(),
                    OfferDate = offer.CreatedAt,
                    ExpirationDate = offer.ExpirationDate,
                    DaysUntilExpiration = (int)(offer.ExpirationDate - now).TotalDays,
                    Terms = offer.Terms,
                    Requirements = offer.Requirements,
                    RequiresGuardianApproval = offer.RequiresGuardianApproval,
                    CanRespond = canRespond,
                    // Use safe navigation
                    RecruiterName = offer.CreatedByStaff != null ? offer.CreatedByStaff.ApplicationUserId : "Unknown",
                    RecruiterEmail = offer.CreatedByStaff != null ? offer.CreatedByStaff.ApplicationUserId + "@university.edu" : "",
                    BandDescription = offer.Band.Description,
                    BandAchievements = offer.Band.Achievements
                });
            }

            return result;
        }

        public async Task<bool> CanRespondToScholarshipAsync(string guardianUserId, int offerId)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null)
                return false;

                        var offer = await _context.Offers
                .Include(so => so.Student)
                .FirstOrDefaultAsync(so => so.Id == offerId); // Changed from Id

            if (offer == null)
                return false;

            return await _context.StudentGuardians.AnyAsync(sg =>
                sg.GuardianId == guardianId.Id &&
                sg.StudentId == offer.StudentId &&
                sg.IsActive &&
                sg.CanRespondToOffers);
        }

        public async Task<GuardianScholarshipDto> RespondToScholarshipAsync(int offerId, string guardianUserId, string response, string? notes)
        {
            var guardianId = await GetGuardianEntityAsync(guardianUserId);
            if (guardianId == null) return new GuardianScholarshipDto();

            // FIX: Validate inputs against Enum expectations
            ScholarshipStatus newStatus;
            if (response == "Accepted") newStatus = ScholarshipStatus.Accepted;
            else if (response == "Declined") newStatus = ScholarshipStatus.Declined;
            else throw new ArgumentException("Response must be 'Accepted' or 'Declined'");

            var offer = await _context.Offers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null) throw new KeyNotFoundException($"Scholarship offer {offerId} not found");

            // FIX: Compare Enum to Enum. "Sent" is the state where students/guardians can respond.
            if (offer.Status != ScholarshipStatus.Sent)
                throw new InvalidOperationException($"Cannot respond to offer in status {offer.Status}");

            if (offer.ExpirationDate < DateTime.UtcNow)
                throw new InvalidOperationException("This offer has expired");

            // Permission check
            var guardianLink = await _context.StudentGuardians
                .FirstOrDefaultAsync(sg => sg.GuardianId == guardianId.Id && sg.StudentId == offer.StudentId && sg.IsActive);

            if (guardianLink == null || !guardianLink.CanRespondToOffers)
                throw new UnauthorizedAccessException("You do not have permission to respond to this offer");

            // FIX: Update Status using Enum
            offer.Status = newStatus;
            offer.ResponseDate = DateTime.UtcNow;
            offer.RespondedByGuardianUserId = guardianUserId;
            offer.ResponseNotes = notes;

            await _context.SaveChangesAsync();
            var now = DateTime.UtcNow;

            return new GuardianScholarshipDto
            {
                OfferId = offer.Id,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandName = offer.Band.BandName,
                University = offer.Band.UniversityName,
                Amount = offer.ScholarshipAmount,
                OfferType = offer.OfferType,
                // Fix: Convert Enum to String
                Status = offer.Status.ToString(),
                OfferDate = offer.CreatedAt,
                ExpirationDate = offer.ExpirationDate,
                DaysUntilExpiration = (int)(offer.ExpirationDate - now).TotalDays,
                Terms = offer.Terms,
                Requirements = offer.Requirements,
                RequiresGuardianApproval = offer.RequiresGuardianApproval,
                CanRespond = false,
                RecruiterName = offer.CreatedByStaff?.ApplicationUserId ?? "Unknown",
                BandDescription = offer.Band.Description
            };
        }

        // =========================================================================
        // NEW HELPER: Used by OffersController (Scenario 2) & VideoService (Scenario 6)
        // =========================================================================
        /// <summary>
        /// Retrieves the UserIds of all guardians linked to a specific student.
        /// Used to send notifications when a student receives an offer or uploads a video.
        /// </summary>
        public async Task<List<string>> GetGuardianUserIdsForStudentAsync(int studentId)
        {
            // Optional: Filter by GuardianNotificationPreferences here if you want to be strict,
            // but usually we fetch all IDs and let the NotificationService check perfs 
            // or send to all for critical items like Offers.

            return await _context.StudentGuardians
                .Where(sg => sg.StudentId == studentId && sg.IsActive && sg.ReceivesNotifications)
                .Include(sg => sg.Guardian)
                .Select(sg => sg.Guardian.ApplicationUserId)
                .ToListAsync();
        }

        public async Task<ServiceResult<NotificationListDto>> GetNotificationsAsync(string userId, NotificationFilterDto filter)
        {
            var guardian = await GetGuardianEntityAsync(userId);
            if (guardian == null) return ServiceResult<NotificationListDto>.Failure("Guardian not found");

            var query = _context.GuardianNotifications.AsQueryable()
                .Where(n => n.GuardianId == guardian.Id);

            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(n => n.Type == filter.Type);

            if (filter.IsRead.HasValue)
                query = query.Where(n => n.IsRead == filter.IsRead.Value);

            if (filter.Since.HasValue)
                query = query.Where(n => n.CreatedAt >= filter.Since.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedDate = n.CreatedAt,
                    IsRead = n.IsRead,
                    IsUrgent = n.IsUrgent,
                    ActionUrl = n.ActionUrl
                })
                .ToListAsync();

            return ServiceResult<NotificationListDto>.Success(new NotificationListDto
            {
                Notifications = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }





        private async Task<List<GuardianRecentActivityDto>> GetRecentActivitiesAcrossStudentsAsync(List<int> studentIds)
        {
            var activities = new List<GuardianRecentActivityDto>();

            // Recent interests
            var interests = await _context.StudentInterests
                .Where(si => studentIds.Contains(si.StudentId))
                .OrderByDescending(si => si.InterestedDate)
                .Take(5)
                .Include(si => si.Student)
                .Include(si => si.Band)
                .Select(si => new GuardianRecentActivityDto
                {
                    ActivityType = "Interest",
                    Description = $"Showed interest in {si.Band.BandName}",
                    Timestamp = si.InterestedDate,
                    StudentId = si.StudentId,
                    StudentName = si.Student.FirstName + " " + si.Student.LastName
                })
                .ToListAsync();

            // Recent offers
            var offers = await _context.Offers
                .Where(so => studentIds.Contains(so.StudentId))
                .OrderByDescending(so => so.CreatedAt)
                .Take(5)
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Select(so => new GuardianRecentActivityDto
                {
                    ActivityType = "Offer",
                    Description = $"Received ${so.ScholarshipAmount} scholarship offer from {so.Band.BandName}",
                    Timestamp = so.CreatedAt,
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName
                })
                .ToListAsync();

            activities.AddRange(interests);
            activities.AddRange(offers);

            return activities.OrderByDescending(a => a.Timestamp).Take(10).ToList();
        }
    }
}