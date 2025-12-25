using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;

namespace Podium.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public StudentService(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _notificationService = notificationService;
        _emailService = emailService;
    }



    /// <summary>
    /// Get student details with authorization checks
    /// </summary>
    public async Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId)
    {
        // Assuming your Repository exposes a way to Include. 
        // If strictly using generic methods: _unitOfWork.Students.GetByIdAsync(studentId);
        // But we need the User data, so we access the Queryable/Set if possible.
        var student = await _unitOfWork.Students.GetQueryable()
            .Include(s => s.ApplicationUser)
            .Include(s => s.Videos)
            .Include(s => s.StudentRatings)
            .Include(s => s.Guardians)
            .Include(s => s.StudentInterests)
            .ThenInclude(si => si.Band) // Required for BandName
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            return ServiceResult<StudentDetailsDto>.Failure("Student not found");

        if (!await CanAccessStudentAsync(studentId))
            return ServiceResult<StudentDetailsDto>.Forbidden("You don't have permission to view this student");

        return ServiceResult<StudentDetailsDto>.Success(MapToDetailsDto(student));
    }

    /// <summary>
    /// Update student profile with authorization checks
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStudentProfileAsync(int studentId, UpdateStudentDto dto)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null)
            return ServiceResult<bool>.Failure("Student not found");

        if (!await _permissionService.IsStudentOwnerAsync(studentId))
            return ServiceResult<bool>.Forbidden("You can only update your own profile");

        // Update Fields
        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Bio = dto.BioDescription;
        student.PrimaryInstrument = dto.PrimaryInstrument; // Updated to match changes
        student.SecondaryInstruments = dto.SecondaryInstruments; // Updated to match changes

        student.PhoneNumber = dto.PhoneNumber;
        student.State = dto.State;
        student.HighSchool = dto.HighSchool;
        student.IntendedMajor = dto.IntendedMajor;
        student.SkillLevel = dto.SkillLevel;
        student.SchoolType = dto.SchoolType;

        if (dto.GraduationYear.HasValue)
            student.GraduationYear = dto.GraduationYear.Value;

        // Serialize Lists to JSON
        if (dto.SecondaryInstruments != null)
            student.SecondaryInstruments = dto.SecondaryInstruments;

        if (dto.Achievements != null)
            student.Achievements = dto.Achievements;

        _unitOfWork.Students.Update(student);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Scenario 1: Student shows interest in a band -> Notify Recruiters
    /// </summary>
    public async Task<ServiceResult<bool>> ShowInterestAsync(int studentId, int bandId)
    {
        var isOwner = await _permissionService.IsStudentOwnerAsync(studentId);
        if (!isOwner)
            return ServiceResult<bool>.Forbidden("You can only express interest for your own profile");

        // Check for existing interest using Repository
        // Assuming FindAsync accepts a predicate
        var existingInterests = await _unitOfWork.StudentInterests.FindAsync(si => si.StudentId == studentId && si.BandId == bandId);
        if (existingInterests.Any())
            return ServiceResult<bool>.Failure("You have already shown interest in this band");

        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        var band = await _unitOfWork.Bands.GetByIdAsync(bandId);

        if (student == null || band == null)
            return ServiceResult<bool>.Failure("Student or Band not found.");

        string studentName = student != null ? $"{student.FirstName} {student.LastName}" : "A student";
        string bandName = band.BandName;


        var interest = new StudentInterest
        {
            StudentId = studentId,
            BandId = bandId,
            IsInterested = true,
            InterestedDate = DateTime.UtcNow
        };

        await _unitOfWork.StudentInterests.AddAsync(interest);
        await _unitOfWork.SaveChangesAsync();

        // Notify Recruiters
        await _notificationService.NotifyBandStaffAsync(
            bandId,
            "NewInterest",
            "New Student Interest",
            $"{studentName} is interested in your band!",
            studentId.ToString(),
            NotificationPriority.High
        );

        // Query for guardians who are Active and opted-in to notifications
        var guardiansToNotify = await _unitOfWork.StudentGuardians.GetQueryable()
            .Include(sg => sg.Guardian)
            .Where(sg => sg.StudentId == studentId
                         && sg.IsActive
                         && sg.ReceivesNotifications)
            .ToListAsync();

        foreach (var sg in guardiansToNotify)
        {
            if (sg.Guardian != null && !string.IsNullOrEmpty(sg.Guardian.ApplicationUserId))
            {
                await _notificationService.NotifyUserAsync(
                    sg.Guardian.ApplicationUserId,
                    "StudentActivity", // Different type for guardian filtering if needed
                    "Student Interest Updated",
                    $"{student.FirstName} has shown interest in {bandName}!", // Personalized for guardian
                    studentId.ToString()
                );
            }
        }


        // ==========================================================
        // SCENARIO A: BAND STAFF (Recruiters)
        // ==========================================================

        // 1. In-App & Push (SignalR)
        await _notificationService.NotifyBandStaffAsync(
            bandId,
            "NewInterest",
            "New Student Interest",
            $"{studentName} is interested in your band!",
            studentId.ToString()
        );

        // 2. Email Notification (For active staff who can view students)
        // We need to fetch them manually to get their Email addresses
        var staffToEmail = await _unitOfWork.BandStaff.GetQueryable()
            .Include(bs => bs.ApplicationUser)
            .Where(bs => bs.BandId == bandId && bs.IsActive && bs.CanViewStudents)
            .ToListAsync();

        foreach (var staff in staffToEmail)
        {
            // Check if user exists and has an email
            if (staff.ApplicationUser != null && !string.IsNullOrEmpty(staff.ApplicationUser.Email))
            {
                // You might want to add a check here for specific Staff Email Preferences if you add that feature later
                await _emailService.SendEmailAsync(
                    staff.ApplicationUser.Email,
                    $"Podium: New Interest from {studentName}",
                    $"Hello {staff.FirstName},<br/><br/>Good news! <strong>{studentName}</strong> has just expressed interest in {bandName}.<br/><br/>Log in to Podium to view their profile."
                );
            }
        }

        // ==========================================================
        // SCENARIO B: GUARDIANS (Parents)
        // ==========================================================

        // Fetch Guardians with their preferences
        var guardians = await _unitOfWork.StudentGuardians.GetQueryable()
            .Include(sg => sg.Guardian)
            .Where(sg => sg.StudentId == studentId && sg.IsActive)
            .ToListAsync();

        foreach (var link in guardians)
        {
            var guardian = link.Guardian;
            if (guardian == null) continue;

            // 1. In-App & Push (SignalR) - ONLY if opted in
            if (link.ReceivesNotifications)
            {
                await _notificationService.NotifyUserAsync(
                    guardian.ApplicationUserId,
                    "StudentActivity",
                    "Student Interest Updated",
                    $"{student.FirstName} has shown interest in {bandName}.",
                    studentId.ToString()
                );
            }

            // 2. Email Notification - ONLY if EmailNotificationsEnabled is true
            if (guardian.EmailNotificationsEnabled && !string.IsNullOrEmpty(guardian.Email))
            {
                await _emailService.SendEmailAsync(
                   guardian.Email,
                   $"Podium Update: {student.FirstName}'s Activity",
                   $"Hello {guardian.FirstName},<br/><br/>Just letting you know that <strong>{student.FirstName}</strong> has shown interest in the <strong>{bandName}</strong> program.<br/><br/>Login to manage preferences."
               );
            }

            // 3. SMS Notification (Placeholder)
            // if (guardian.SmsNotificationsEnabled && _smsService != null) { ... }
        }


        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> RateStudentAsync(int studentId, RatingDto dto)
    {
        // 1. Verify Permission
        if (!await _permissionService.HasPermissionAsync(Permissions.RateStudents))
            return ServiceResult<bool>.Forbidden("You do not have permission to rate students.");

        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null) return ServiceResult<bool>.Failure("Student not found.");

        // 2. Identify the Rater (BandStaff)
        var userId = await _permissionService.GetCurrentUserIdAsync();
        var staffMembers = await _unitOfWork.BandStaff.FindAsync(bs => bs.ApplicationUserId == userId);
        var bandStaff = staffMembers.FirstOrDefault();

        if (bandStaff == null)
            return ServiceResult<bool>.Failure("Rater profile not found.");

        // 3. Save Rating
        var rating = new StudentRating
        {
            StudentId = studentId,
            BandStaffId = bandStaff.Id,
            Rating = dto.Rating,
            Comments = dto.Comments,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StudentRatings.AddAsync(rating);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }


    /// <summary>
    /// Get all students the current user can access
    /// </summary>
    public async Task<ServiceResult<PagedResult<StudentDetailsDto>>> GetAccessibleStudentsAsync(int page = 1, int pageSize = 20)
    {
        var role = await _permissionService.GetCurrentUserRoleAsync();
        var userId = await _permissionService.GetCurrentUserIdAsync();

        if (userId == null) return ServiceResult<PagedResult<StudentDetailsDto>>.Failure("User not authenticated");

        // Start with base query including User
        IQueryable<Student> query = _unitOfWork.Students.GetQueryable().Include(s => s.ApplicationUser);

        // Apply Authorization Filters
        switch (role)
        {
            case Roles.Student:
                query = query.Where(s => s.ApplicationUserId == userId);
                break;
            case Roles.Guardian:
                var guardianQuery = _unitOfWork.Guardians.GetQueryable()
                    .Include(g => g.Students)
                    .Where(g => g.ApplicationUserId == userId);

                var guardian = await guardianQuery.FirstOrDefaultAsync();

                if (guardian?.Students == null || !guardian.Students.Any())
                {
                    return ServiceResult<PagedResult<StudentDetailsDto>>.Success(new PagedResult<StudentDetailsDto>
                    {
                        Items = new List<StudentDetailsDto>(),
                        TotalCount = 0,
                        PageNumber = page,
                        PageSize = pageSize
                    });
                }

                var studentIds = guardian.Students.Select(s => s.Id).ToList();
                query = query.Where(s => studentIds.Contains(s.Id));
                break;
            case Roles.BandStaff:
            case Roles.Director:
                if (!await _permissionService.HasPermissionAsync(Permissions.ViewStudents))
                    return ServiceResult<PagedResult<StudentDetailsDto>>.Forbidden("No permission");
                break;
            default:
                return ServiceResult<PagedResult<StudentDetailsDto>>.Forbidden("No permission");
        }

        // 1. Get Total Count (before paging)
        var totalCount = await query.CountAsync();

        // 2. Apply Pagination
        var students = await query
            .OrderBy(s => s.LastName) // Ensure consistent ordering for pagination
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = students.Select(MapToDetailsDto).ToList();

        // 3. Return Paged Result
        var result = new PagedResult<StudentDetailsDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<StudentDetailsDto>>.Success(result);
    }

    private StudentDetailsDto MapToDetailsDto(Student s)
    {


        // Calculate Ratings
        double avgRating = 0;
        int ratingCount = 0;
        if (s.StudentRatings != null && s.StudentRatings.Any())
        {
            avgRating = s.StudentRatings.Average(r => r.Rating);
            ratingCount = s.StudentRatings.Count;
        }

        // Get Primary Video
        var primaryVideo = s.Videos?.FirstOrDefault(v => v.IsPrimary && !v.IsDeleted)
                           ?? s.Videos?.FirstOrDefault(v => !v.IsDeleted);

        return new StudentDetailsDto
        {
            StudentId = s.Id,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.ApplicationUser?.Email ?? s.Email,

            PrimaryInstrument = s.PrimaryInstrument,
            Bio = s.Bio,
            GPA = s.GPA,

            // Personal
            PhoneNumber = s.PhoneNumber,
            DateOfBirth = null, // Add s.DateOfBirth to Entity if needed
            State = s.State,
            City = null,        // Add s.City to Entity if needed
            Zipcode = null,     // Add s.Zipcode to Entity if needed

            // Academic
            HighSchool = s.HighSchool,
            GraduationYear = s.GraduationYear,
            IntendedMajor = s.IntendedMajor,
            SkillLevel = s.SkillLevel,
            SchoolType = s.SchoolType,
            YearsExperience = s.YearsExperience,

            // Lists
            SecondaryInstruments = s.SecondaryInstruments,
            Achievements = s.Achievements,
            Interests = s.StudentInterests?.Select(si => new StudentInterestDetailDto
            {
                BandId = si.BandId,
                BandName = si.Band?.BandName ?? "Unknown Band", // Requires .Include(s => s.StudentInterests).ThenInclude(si => si.Band)
                InterestedAt = si.InterestedDate
            }).ToList() ?? new(),

            // Metrics
            VideoUrl = primaryVideo?.Url,
            VideoThumbnailUrl = primaryVideo?.ThumbnailUrl,
            AverageRating = avgRating,
            RatingCount = ratingCount,
            ProfileViews = 0, // Implement ProfileView logic if needed
            HasGuardian = s.Guardians.Any(),

            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    /// <summary>
    /// Private helper to check if current user can access a student
    /// </summary>
    private async Task<bool> CanAccessStudentAsync(int studentId)
    {
        var role = await _permissionService.GetCurrentUserRoleAsync();
        switch (role)
        {
            case Roles.Student: return await _permissionService.IsStudentOwnerAsync(studentId);
            case Roles.Guardian: return await _permissionService.IsGuardianOfStudentAsync(studentId);
            case Roles.BandStaff:
            case Roles.Director: return await _permissionService.HasPermissionAsync(Permissions.ViewStudents);
            default: return false;
        }
    }

    public async Task<ServiceResult<StudentDashboardDto>> GetStudentDashboardAsync()
    {
        // 1. Identify the current user
        var userId = await _permissionService.GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return ServiceResult<StudentDashboardDto>.Failure("User not authenticated");

        // 2. Fetch Student with related data needed for metrics
        var student = await _unitOfWork.Students.GetQueryable()
            .Include(s => s.Videos)
            .Include(s => s.ScholarshipOffers)
            .Include(s => s.ContactRequests)
            .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

        if (student == null)
            return ServiceResult<StudentDashboardDto>.Failure("Student profile not found");

        // 3. Calculate Metrics

        // Offers: Assuming active offers are those not declined, rescinded, or expired.
        // Adjust logic based on your ScholarshipStatus enum.
        var activeOffersCount = student.ScholarshipOffers
            .Count(o => o.Status != ScholarshipStatus.Declined &&
                        o.Status != ScholarshipStatus.Rescinded &&
                        o.Status != ScholarshipStatus.Draft);

        // Contact Requests
        var pendingRequestsCount = await _unitOfWork.ContactRequests.GetQueryable()
    .Where(c => c.Status == "Pending") // <--- Use the Status property
    .ToListAsync();

        // Notifications
        var recentNotifications = await _unitOfWork.Notifications.GetQueryable()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new StudentNotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();

        // Profile Image (Primary Video Thumbnail)
        var profileImage = student.Videos
            .Where(v => !v.IsDeleted && v.ThumbnailUrl != null)
            .OrderByDescending(v => v.IsPrimary)
            .Select(v => v.ThumbnailUrl)
            .FirstOrDefault();

        // 4. Construct Activity Feed (Example: mixing offers and notifications)
        var activities = new List<StudentActivityDto>();

        // Add recent offers to activity
        activities.AddRange(student.ScholarshipOffers
            .Where(o => o.CreatedAt > DateTime.UtcNow.AddDays(-30))
            .Select(o => new StudentActivityDto
            {
                Description = $"Received scholarship offer", // You can enrich this if you include Band
                Date = o.CreatedAt,
                Icon = "local_offer"
            }));

        // Add recent notifications to activity
        activities.AddRange(recentNotifications.Select(n => new StudentActivityDto
        {
            Description = n.Title,
            Date = n.CreatedAt,
            Icon = n.Type.ToLower().Contains("alert") ? "warning" : "notifications"
        }));

        // Sort combined activity
        var sortedActivity = activities.OrderByDescending(a => a.Date).Take(10).ToList();

        // 5. Build DTO
        var dashboardDto = new StudentDashboardDto
        {
            StudentId = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            PrimaryInstrument = student.PrimaryInstrument ?? "Unknown",
            ProfileImageUrl = profileImage,
            GuardianInviteCode = student.GuardianInviteCode,

            // Metrics
            ActiveOffers = activeOffersCount,
            PendingContactRequests = pendingRequestsCount.Count,
            TotalProfileViews = 0, // Requires adding IRepository<ProfileView> to UnitOfWork to fetch real count
            SearchAppearances = 0, // Requires logging search results

            RecentNotifications = recentNotifications,
            RecentActivity = sortedActivity
        };

        return ServiceResult<StudentDashboardDto>.Success(dashboardDto);
    }
}
