using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;
using System.Linq;
using System.Text.Json;

namespace Podium.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly INotificationService _notificationService;

    public StudentService(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _notificationService = notificationService;
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
        student.Bio = dto.Bio;
        student.Instrument = dto.Instrument;
        student.PrimaryInstrument = dto.Instrument; // Sync redundancy

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
            student.SecondaryInstruments = JsonSerializer.Serialize(dto.SecondaryInstruments);

        if (dto.Achievements != null)
            student.Achievements = JsonSerializer.Serialize(dto.Achievements);

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
            "A new student has shown interest in your band!",
            studentId.ToString()
        );

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
        List<string> secondary = new();
        List<string> achievements = new();

        try { if (!string.IsNullOrEmpty(s.SecondaryInstruments)) secondary = JsonSerializer.Deserialize<List<string>>(s.SecondaryInstruments) ?? new(); } catch { }
        try { if (!string.IsNullOrEmpty(s.Achievements)) achievements = JsonSerializer.Deserialize<List<string>>(s.Achievements) ?? new(); } catch { }

        return new StudentDetailsDto
        {
            StudentId = s.Id,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.ApplicationUser?.Email ?? s.Email,
            Instrument = s.Instrument,
            Bio = s.Bio,
            GPA = s.GPA,
            PhoneNumber = s.PhoneNumber,
            State = s.State,
            HighSchool = s.HighSchool,
            GraduationYear = s.GraduationYear,
            IntendedMajor = s.IntendedMajor,
            SkillLevel = s.SkillLevel,
            SchoolType = s.SchoolType,
            SecondaryInstruments = secondary,
            Achievements = achievements
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
}
