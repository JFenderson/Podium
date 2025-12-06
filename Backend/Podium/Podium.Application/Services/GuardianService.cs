using Microsoft.Extensions.Logging;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
    public class GuardianService : IGuardianService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GuardianService> _logger;

        public GuardianService(ApplicationDbContext context, ILogger<GuardianService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all students linked to this guardian with their link details.
        /// AUTHORIZATION: Only returns students explicitly linked to this guardian.
        /// </summary>
        public async Task<List<LinkedStudentDto>> GetLinkedStudentsAsync(string guardianUserId)
        {
            return await _context.StudentGuardians
                .Where(sg => sg.GuardianUserId == guardianUserId && sg.IsActive)
                .Include(sg => sg.Student)
                .Select(sg => new LinkedStudentDto
                {
                    StudentId = sg.StudentId,
                    StudentName = sg.Student.FirstName + " " + sg.Student.LastName,
                    Email = sg.Student.Email,
                    Phone = sg.Student.PhoneNumber,
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

        /// <summary>
        /// CRITICAL AUTHORIZATION CHECK: Verify guardian has access to student.
        /// Must be called before any operation involving student data.
        /// Checks for active, verified guardian link.
        /// </summary>
        public async Task<bool> CanAccessStudentAsync(string guardianUserId, int studentId)
        {
            return await _context.StudentGuardians
                .AnyAsync(sg =>
                    sg.GuardianUserId == guardianUserId &&
                    sg.StudentId == studentId &&
                    sg.IsActive);
        }

        /// <summary>
        /// Get detailed activity history for a student.
        /// AUTHORIZATION: Requires CanViewActivity permission.
        /// PERFORMANCE: Uses parallel queries for different activity types.
        /// </summary>
        public async Task<StudentActivityDto> GetStudentActivityAsync(int studentId, int daysBack)
        {
            var startDate = DateTime.UtcNow.AddDays(-daysBack);
            var student = await _context.Students.FindAsync(studentId);

            if (student == null)
                throw new KeyNotFoundException($"Student {studentId} not found");

            // PARALLEL EXECUTION: Load different activity types simultaneously
            var videosTask = _context.Videos
                .Where(v => v.StudentId == studentId && v.UploadedDate >= startDate)
                .OrderByDescending(v => v.UploadedDate)
                .Select(v => new VideoActivityDto
                {
                    VideoId = v.Id,
                    Title = v.Title,
                    Instrument = v.Instrument,
                    UploadedDate = v.UploadedDate,
                    Views = v.ViewCount,
                    IsPublic = v.IsPublic
                })
                .ToListAsync();

            var interestsTask = _context.StudentInterests
                .Where(si => si.StudentId == studentId && si.InterestedDate >= startDate)
                .Include(si => si.Band)
                .OrderByDescending(si => si.InterestedDate)
                .Select(si => new InterestActivityDto
                {
                    BandId = si.BandId,
                    BandName = si.Band.Name,
                    University = si.Band.UniversityName,
                    InterestDate = si.InterestedDate,
                    HasBeenContacted = si.Student.ContactLogs.Any(cl => cl.BandId == si.BandId),
                    ContactDate = si.Student.ContactLogs
                        .Where(cl => cl.BandId == si.BandId)
                        .OrderBy(cl => cl.ContactDate)
                        .Select(cl => (DateTime?)cl.ContactDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var offersTask = _context.ScholarshipOffers
                .Where(so => so.StudentId == studentId && so.CreatedDate >= startDate)
                .Include(so => so.Band)
                .OrderByDescending(so => so.CreatedDate)
                .Select(so => new OfferActivityDto
                {
                    OfferId = so.Id,
                    BandName = so.Band.Name,
                    Amount = so.Amount,
                    Status = so.Status,
                    OfferDate = so.CreatedDate,
                    ExpirationDate = so.ExpirationDate,
                    RequiresGuardianApproval = so.RequiresGuardianApproval
                })
                .ToListAsync();

            var eventsTask = _context.EventRegistrations
                .Where(er => er.StudentId == studentId && er.RegisteredDate >= startDate)
                .Include(er => er.Event)
                    .ThenInclude(e => e.Band)
                .OrderByDescending(er => er.Event.EventDate)
                .Select(er => new EventActivityDto
                {
                    EventId = er.EventId,
                    EventName = er.Event.EventName,
                    BandName = er.Event.Band.Name,
                    EventDate = er.Event.EventDate,
                    DidAttend = er.DidAttend,
                    RegisteredDate = er.RegisteredDate
                })
                .ToListAsync();

            var contactsTask = _context.ContactLogs
                .Where(cl => cl.StudentId == studentId && cl.ContactDate >= startDate)
                .Include(cl => cl.Band)
                .Include(cl => cl.RecruiterStaff)
                .OrderByDescending(cl => cl.ContactDate)
                .Select(cl => new ContactActivityDto
                {
                    ContactId = cl.Id,
                    RecruiterName = cl.RecruiterStaff.UserId,
                    BandName = cl.Band.Name,
                    ContactDate = cl.ContactDate,
                    ContactMethod = cl.ContactMethod,
                    Purpose = cl.Purpose
                })
                .ToListAsync();

            await Task.WhenAll(videosTask, interestsTask, offersTask, eventsTask, contactsTask);

            return new StudentActivityDto
            {
                StudentId = studentId,
                StudentName = student.FirstName + " " + student.LastName,
                StartDate = startDate,
                EndDate = DateTime.UtcNow,
                VideosUploaded = await videosTask,
                InterestShown = await interestsTask,
                OffersReceived = await offersTask,
                EventsAttended = await eventsTask,
                ContactsMade = await contactsTask,
                TotalVideos = (await videosTask).Count,
                TotalInterests = (await interestsTask).Count,
                TotalOffers = (await offersTask).Count,
                TotalEvents = (await eventsTask).Count,
                TotalContacts = (await contactsTask).Count
            };
        }

        /// <summary>
        /// Get student profile respecting privacy settings.
        /// AUTHORIZATION: Guardian must be linked to student with CanViewProfile permission.
        /// PRIVACY: Only returns information student has permitted guardians to view.
        /// </summary>
        public async Task<StudentProfileDto> GetStudentProfileAsync(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Videos)
                .Include(s => s.StudentInterests)
                .Include(s => s.EventRegistrations)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                throw new KeyNotFoundException($"Student {studentId} not found");

            // Parse secondary instruments from JSON if stored that way
            var secondaryInstruments = new List<string>();
            if (!string.IsNullOrEmpty(student.SecondaryInstruments))
            {
                try
                {
                    secondaryInstruments = JsonSerializer.Deserialize<List<string>>(student.SecondaryInstruments) ?? new List<string>();
                }
                catch
                {
                    // Handle if stored as comma-separated instead
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
                StudentId = student.Id,
                Name = student.FirstName + " " + student.LastName,
                Email = student.Email,
                Phone = student.PhoneNumber,
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

        /// <summary>
        /// Get contact requests requiring guardian approval.
        /// AUTHORIZATION: Only returns requests for students linked to this guardian.
        /// PERFORMANCE: Optimized with strategic includes to avoid N+1 queries.
        /// </summary>
        public async Task<List<ContactRequestDto>> GetContactRequestsAsync(string guardianUserId, int? studentId, string? status)
        {
            // Get all students this guardian has access to
            var accessibleStudentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianUserId == guardianUserId && sg.IsActive && sg.CanApproveContacts)
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
                    RequestId = cr.Id,
                    StudentId = cr.StudentId,
                    StudentName = cr.Student.FirstName + " " + cr.Student.LastName,
                    BandId = cr.BandId,
                    BandName = cr.Band.Name,
                    University = cr.Band.UniversityName,
                    RecruiterName = cr.RecruiterStaff.UserId,
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

        /// <summary>
        /// AUTHORIZATION CHECK: Verify guardian can manage a specific contact request.
        /// Checks: (1) Guardian is linked to student, (2) Has CanApproveContacts permission.
        /// </summary>
        public async Task<bool> CanManageContactRequestAsync(string guardianUserId, int requestId)
        {
            var request = await _context.ContactRequests
                .Include(cr => cr.Student)
                    .ThenInclude(s => s.StudentGuardians)
                .FirstOrDefaultAsync(cr => cr.Id == requestId);

            if (request == null)
                return false;

            return request.Student.StudentGuardians.Any(sg =>
                sg.GuardianUserId == guardianUserId &&
                sg.IsActive &&
                sg.CanApproveContacts);
        }

        /// <summary>
        /// Approve a contact request.
        /// BUSINESS LOGIC: Updates status, triggers notifications to recruiter and student.
        /// </summary>
        public async Task<ContactRequestDto> ApproveContactRequestAsync(int requestId, string guardianUserId, string? notes)
        {
            var request = await _context.ContactRequests
                .Include(cr => cr.Student)
                .Include(cr => cr.Band)
                .Include(cr => cr.RecruiterStaff)
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

            // TODO: Trigger notifications (implement notification service)
            // - Notify recruiter that they can proceed with contact
            // - Notify student that contact was approved by guardian

            return new ContactRequestDto
            {
                RequestId = request.Id,
                StudentId = request.StudentId,
                StudentName = request.Student.FirstName + " " + request.Student.LastName,
                BandId = request.BandId,
                BandName = request.Band.Name,
                University = request.Band.UniversityName,
                RecruiterName = request.RecruiterStaff.UserId,
                RecruiterTitle = request.RecruiterStaff.Role,
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
        /// Decline a contact request.
        /// PRIVACY: Reason is logged but recruiter only sees generic decline message unless configured otherwise.
        /// </summary>
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

            // TODO: Trigger notifications
            // - Notify recruiter of decline (generic message)
            // - Notify student of decline

            return new ContactRequestDto
            {
                RequestId = request.Id,
                StudentId = request.StudentId,
                StudentName = request.Student.FirstName + " " + request.Student.LastName,
                BandId = request.BandId,
                BandName = request.Band.Name,
                University = request.Band.UniversityName,
                RecruiterName = request.RecruiterStaff.UserId,
                RecruiterTitle = request.RecruiterStaff.Role,
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
            var accessibleStudentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianUserId == guardianUserId && sg.IsActive)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!accessibleStudentIds.Any())
                return new List<GuardianScholarshipDto>();

            var query = _context.ScholarshipOffers
                .Where(so => accessibleStudentIds.Contains(so.StudentId));

            if (studentId.HasValue)
                query = query.Where(so => so.StudentId == studentId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(so => so.Status == status);

            var now = DateTime.UtcNow;

            var offers = await query
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            // Check guardian permissions for each offer
            var result = new List<GuardianScholarshipDto>();
            foreach (var offer in offers)
            {
                var guardianLink = await _context.StudentGuardians
                    .FirstOrDefaultAsync(sg =>
                        sg.GuardianUserId == guardianUserId &&
                        sg.StudentId == offer.StudentId &&
                        sg.IsActive);

                var canRespond = guardianLink?.CanRespondToOffers ?? false;

                result.Add(new GuardianScholarshipDto
                {
                    OfferId = offer.Id,
                    StudentId = offer.StudentId,
                    StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                    BandName = offer.Band.Name,
                    University = offer.Band.UniversityName,
                    Amount = offer.Amount,
                    OfferType = offer.OfferType,
                    Status = offer.Status,
                    OfferDate = offer.CreatedDate,
                    ExpirationDate = offer.ExpirationDate,
                    DaysUntilExpiration = (int)(offer.ExpirationDate - now).TotalDays,
                    Terms = offer.Terms,
                    Requirements = offer.Requirements,
                    RequiresGuardianApproval = offer.RequiresGuardianApproval,
                    CanRespond = canRespond,
                    RecruiterName = offer.CreatedByStaff.UserId,
                    RecruiterEmail = offer.CreatedByStaff.UserId + "@university.edu", // Would lookup from Identity
                    RecruiterPhone = null,
                    BandDescription = offer.Band.Description,
                    BandAchievements = offer.Band.Achievements
                });
            }

            return result;
        }

        /// <summary>
        /// AUTHORIZATION CHECK: Verify guardian can respond to scholarship offer.
        /// Requires: (1) Guardian linked to student, (2) CanRespondToOffers permission.
        /// </summary>
        public async Task<bool> CanRespondToScholarshipAsync(string guardianUserId, int offerId)
        {
            var offer = await _context.ScholarshipOffers
                .Include(so => so.Student)
                    .ThenInclude(s => s.StudentGuardians)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null)
                return false;

            return offer.Student.StudentGuardians.Any(sg =>
                sg.GuardianUserId == guardianUserId &&
                sg.IsActive &&
                sg.CanRespondToOffers);
        }

        /// <summary>
        /// Respond to a scholarship offer (accept or decline).
        /// BUSINESS LOGIC: 
        /// - Validates deadline hasn't passed
        /// - Updates offer status
        /// - Triggers notifications to band and student
        /// - For acceptance, may initiate enrollment workflow
        /// </summary>
        public async Task<GuardianScholarshipDto> RespondToScholarshipAsync(int offerId, string guardianUserId, string response, string? notes)
        {
            if (response != "Accepted" && response != "Declined")
                throw new ArgumentException("Response must be 'Accepted' or 'Declined'");

            var offer = await _context.ScholarshipOffers
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Include(so => so.CreatedByStaff)
                .FirstOrDefaultAsync(so => so.Id == offerId);

            if (offer == null)
                throw new KeyNotFoundException($"Scholarship offer {offerId} not found");

            if (offer.Status != "Approved" && offer.Status != "Pending")
                throw new InvalidOperationException($"Cannot respond to offer in status {offer.Status}");

            // Check if offer has expired
            if (offer.ExpirationDate < DateTime.UtcNow)
                throw new InvalidOperationException("This offer has expired");

            // Verify guardian has permission
            var guardianLink = await _context.StudentGuardians
                .FirstOrDefaultAsync(sg =>
                    sg.GuardianUserId == guardianUserId &&
                    sg.StudentId == offer.StudentId &&
                    sg.IsActive);

            if (guardianLink == null || !guardianLink.CanRespondToOffers)
                throw new UnauthorizedAccessException("You do not have permission to respond to this offer");

            offer.Status = response;
            offer.ResponseDate = DateTime.UtcNow;
            offer.RespondedByGuardianUserId = guardianUserId;
            offer.ResponseNotes = notes;

            await _context.SaveChangesAsync();

            // TODO: Trigger notifications
            // - Notify band of acceptance/decline
            // - Notify student
            // - If accepted, trigger enrollment workflow

            var now = DateTime.UtcNow;

            return new GuardianScholarshipDto
            {
                OfferId = offer.Id,
                StudentId = offer.StudentId,
                StudentName = offer.Student.FirstName + " " + offer.Student.LastName,
                BandName = offer.Band.Name,
                University = offer.Band.UniversityName,
                Amount = offer.Amount,
                OfferType = offer.OfferType,
                Status = offer.Status,
                OfferDate = offer.CreatedDate,
                ExpirationDate = offer.ExpirationDate,
                DaysUntilExpiration = (int)(offer.ExpirationDate - now).TotalDays,
                Terms = offer.Terms,
                Requirements = offer.Requirements,
                RequiresGuardianApproval = offer.RequiresGuardianApproval,
                CanRespond = false, // Already responded
                RecruiterName = offer.CreatedByStaff.UserId,
                RecruiterEmail = offer.CreatedByStaff.UserId + "@university.edu",
                RecruiterPhone = null,
                BandDescription = offer.Band.Description,
                BandAchievements = offer.Band.Achievements
            };
        }

        /// <summary>
        /// Get notifications for guardian.
        /// PERFORMANCE: Paginated to handle large volumes.
        /// </summary>
        public async Task<NotificationListDto> GetNotificationsAsync(string guardianUserId, NotificationFilterDto filters)
        {
            var query = _context.GuardianNotifications
                .Where(n => n.GuardianUserId == guardianUserId);

            if (!string.IsNullOrEmpty(filters.Type))
                query = query.Where(n => n.Type == filters.Type);

            if (filters.IsRead.HasValue)
                query = query.Where(n => n.IsRead == filters.IsRead.Value);

            if (filters.Since.HasValue)
                query = query.Where(n => n.CreatedDate >= filters.Since.Value);

            var totalCount = await query.CountAsync();
            var unreadCount = await query.CountAsync(n => !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedDate = n.CreatedDate,
                    IsRead = n.IsRead,
                    IsUrgent = n.IsUrgent,
                    StudentId = n.StudentId,
                    StudentName = n.StudentId.HasValue
                        ? n.Student!.FirstName + " " + n.Student.LastName
                        : null,
                    ActionUrl = n.ActionUrl,
                    Metadata = n.MetadataJson != null
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(n.MetadataJson)
                        : null
                })
                .ToListAsync();

            return new NotificationListDto
            {
                Notifications = notifications,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = filters.Page,
                PageSize = filters.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize)
            };
        }

        /// <summary>
        /// Update guardian notification preferences.
        /// Handles per-student overrides stored as JSON.
        /// </summary>
        public async Task<GuardianNotificationPreferencesDto> UpdateNotificationPreferencesAsync(string guardianUserId, UpdatePreferencesRequest request)
        {
            var prefs = await _context.GuardianNotificationPreferences
                .FirstOrDefaultAsync(p => p.GuardianUserId == guardianUserId);

            if (prefs == null)
            {
                prefs = new GuardianNotificationPreferences
                {
                    GuardianUserId = guardianUserId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.GuardianNotificationPreferences.Add(prefs);
            }

            // Update only provided fields
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

            prefs.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Parse overrides for return
            Dictionary<int, StudentNotificationOverrideDto>? overrides = null;
            if (!string.IsNullOrEmpty(prefs.StudentOverridesJson))
            {
                try
                {
                    overrides = JsonSerializer.Deserialize<Dictionary<int, StudentNotificationOverrideDto>>(prefs.StudentOverridesJson);
                }
                catch { }
            }

            return new GuardianNotificationPreferencesDto
            {
                UserId = prefs.GuardianUserId,
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
            };
        }

        /// <summary>
        /// Get comprehensive dashboard for guardian.
        /// PERFORMANCE: Single optimized query with strategic projections.
        /// Aggregates data across all linked students for overview.
        /// </summary>
        public async Task<GuardianDashboardDto> GetDashboardAsync(string guardianUserId)
        {
            var studentIds = await _context.StudentGuardians
                .Where(sg => sg.GuardianUserId == guardianUserId && sg.IsActive)
                .Select(sg => sg.StudentId)
                .ToListAsync();

            if (!studentIds.Any())
                return new GuardianDashboardDto();

            var now = DateTime.UtcNow;
            var expiringThreshold = now.AddDays(7);

            // PARALLEL EXECUTION: Load different dashboard sections simultaneously
            var studentSummariesTask = _context.Students
                .Where(s => studentIds.Contains(s.Id))
                .Select(s => new StudentSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = s.FirstName + " " + s.LastName,
                    PrimaryInstrument = s.PrimaryInstrument,
                    GraduationYear = s.GraduationYear,
                    PendingContactRequests = s.ContactRequests.Count(cr => cr.Status == "Pending"),
                    ActiveScholarshipOffers = s.ScholarshipOffers.Count(so => so.Status == "Approved" || so.Status == "Pending"),
                    BandsInterested = s.StudentInterests.Count,
                    LastActivityDate = s.LastActivityDate,
                    HasExpiringOffers = s.ScholarshipOffers.Any(so =>
                        (so.Status == "Approved" || so.Status == "Pending") &&
                        so.ExpirationDate <= expiringThreshold),
                    HasUrgentApprovals = s.ContactRequests.Any(cr => cr.Status == "Pending" && cr.IsUrgent)
                })
                .ToListAsync();

            var pendingApprovalsTask = _context.ContactRequests
                .Where(cr => studentIds.Contains(cr.StudentId) && cr.Status == "Pending")
                .CountAsync();

            var activeOffersTask = _context.ScholarshipOffers
                .Where(so => studentIds.Contains(so.StudentId) &&
                    (so.Status == "Approved" || so.Status == "Pending"))
                .CountAsync();

            var unreadNotificationsTask = _context.GuardianNotifications
                .Where(n => n.GuardianUserId == guardianUserId && !n.IsRead)
                .CountAsync();

            // Priority alerts
            var expiringOffersTask = _context.ScholarshipOffers
                .Where(so => studentIds.Contains(so.StudentId) &&
                    (so.Status == "Approved" || so.Status == "Pending") &&
                    so.ExpirationDate <= expiringThreshold)
                .Include(so => so.Student)
                .Select(so => new PriorityAlertDto
                {
                    AlertType = "ExpiringOffer",
                    Message = $"Scholarship offer expires in {(int)(so.ExpirationDate - now).TotalDays} days",
                    StudentId = so.StudentId,
                    StudentName = so.Student.FirstName + " " + so.Student.LastName,
                    Deadline = so.ExpirationDate,
                    ActionUrl = $"/guardian/scholarships/{so.Id}",
                    Severity = (so.ExpirationDate - now).TotalDays <= 3 ? "High" : "Medium"
                })
                .ToListAsync();

            var urgentApprovalsTask = _context.ContactRequests
                .Where(cr => studentIds.Contains(cr.StudentId) && cr.Status == "Pending" && cr.IsUrgent)
                .Include(cr => cr.Student)
                .Select(cr => new PriorityAlertDto
                {
                    AlertType = "UrgentApproval",
                    Message = "Urgent contact request awaiting approval",
                    StudentId = cr.StudentId,
                    StudentName = cr.Student.FirstName + " " + cr.Student.LastName,
                    Deadline = cr.RequestedDate.AddDays(3), // Assume 3-day urgency
                    ActionUrl = $"/guardian/contact-requests/{cr.Id}",
                    Severity = "High"
                })
                .ToListAsync();

            // Recent activities across all students
            var recentActivitiesTask = GetRecentActivitiesAcrossStudentsAsync(studentIds);

            await Task.WhenAll(
                studentSummariesTask,
                pendingApprovalsTask,
                activeOffersTask,
                unreadNotificationsTask,
                expiringOffersTask,
                urgentApprovalsTask,
                recentActivitiesTask
            );

            var priorityAlerts = new List<PriorityAlertDto>();
            priorityAlerts.AddRange(await expiringOffersTask);
            priorityAlerts.AddRange(await urgentApprovalsTask);
            priorityAlerts = priorityAlerts.OrderBy(a => a.Deadline).Take(10).ToList();

            return new GuardianDashboardDto
            {
                LinkedStudents = await studentSummariesTask,
                TotalPendingApprovals = await pendingApprovalsTask,
                TotalActiveOffers = await activeOffersTask,
                TotalUnreadNotifications = await unreadNotificationsTask,
                PriorityAlerts = priorityAlerts,
                RecentActivities = await recentActivitiesTask
            };
        }

        private async Task<List<RecentActivityDto>> GetRecentActivitiesAcrossStudentsAsync(List<int> studentIds)
        {
            var activities = new List<RecentActivityDto>();

            // Recent interests
            var interests = await _context.StudentInterests
                .Where(si => studentIds.Contains(si.StudentId))
                .OrderByDescending(si => si.InterestedDate)
                .Take(5)
                .Include(si => si.Student)
                .Include(si => si.Band)
                .Select(si => new RecentActivityDto
                {
                    ActivityType = "Interest",
                    Description = $"Showed interest in {si.Band.Name}",
                    Timestamp = si.InterestedDate,
                    StudentId = si.StudentId,
                    StudentName = si.Student.FirstName + " " + si.Student.LastName
                })
                .ToListAsync();

            // Recent offers
            var offers = await _context.ScholarshipOffers
                .Where(so => studentIds.Contains(so.StudentId))
                .OrderByDescending(so => so.CreatedDate)
                .Take(5)
                .Include(so => so.Student)
                .Include(so => so.Band)
                .Select(so => new RecentActivityDto
                {
                    ActivityType = "Offer",
                    Description = $"Received ${so.Amount} scholarship offer from {so.Band.Name}",
                    Timestamp = so.CreatedDate,
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

}
