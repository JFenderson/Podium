using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;


namespace Podium.Application.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public StudentService(
        ApplicationDbContext context,
        IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    

    /// <summary>
    /// Get student details with authorization checks
    /// </summary>
    public async Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId)
    {
        var student = await _context.Students
            .Include(s => s.ApplicationUser)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (student == null)
        {
            return ServiceResult<StudentDetailsDto>.Failure("Student not found");
        }

        // Check if current user can access this student
        var canAccess = await CanAccessStudentAsync(studentId);
        if (!canAccess)
        {
            return ServiceResult<StudentDetailsDto>.Forbidden("You don't have permission to view this student");
        }

        var dto = new StudentDetailsDto
        {
            StudentId = student.StudentId,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.ApplicationUser?.Email ?? student.Email,
            Instrument = student.Instrument,
            Bio = student.Bio,
            GPA = student.GPA
        };

        return ServiceResult<StudentDetailsDto>.Success(dto);
    }

    /// <summary>
    /// Update student profile with authorization checks
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStudentProfileAsync(int studentId, UpdateStudentDto dto)
    {
        var student = await _context.Students.FindAsync(studentId);
        if (student == null)
        {
            return ServiceResult<bool>.Failure("Student not found");
        }

        // Only students can update their own profile
        var isOwner = await _permissionService.IsStudentOwnerAsync(studentId);
        if (!isOwner)
        {
            return ServiceResult<bool>.Forbidden("You can only update your own profile");
        }

        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Bio = dto.Bio;
        student.Instrument = dto.Instrument;

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Get all students the current user can access
    /// </summary>
    public async Task<ServiceResult<IEnumerable<StudentDetailsDto>>> GetAccessibleStudentsAsync()
    {
        var role = await _permissionService.GetCurrentUserRoleAsync();
        var userId = await _permissionService.GetCurrentUserIdAsync();

        if (userId == null)
        {
            return ServiceResult<IEnumerable<StudentDetailsDto>>.Failure("User not authenticated");
        }

        IQueryable<Student> query = _context.Students;

        switch (role)
        {
            case Roles.Student:
                // Students can only see themselves - using string ApplicationUserId
                query = query.Where(s => s.ApplicationUserId == userId);
                break;

            case Roles.Guardian:
                // Guardians can see their linked students
                var guardian = await _context.Guardians
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);

                if (guardian?.Students == null)
                {
                    return ServiceResult<IEnumerable<StudentDetailsDto>>.Success(
                        Enumerable.Empty<StudentDetailsDto>());
                }

                var studentIds = guardian.Students.Select(s => s.StudentId).ToList();
                query = query.Where(s => studentIds.Contains(s.StudentId));
                break;

            case Roles.Recruiter:
            case Roles.Director:
                // BandStaff must have ViewStudents permission
                if (!await _permissionService.HasPermissionAsync(Permissions.ViewStudents))
                {
                    return ServiceResult<IEnumerable<StudentDetailsDto>>.Forbidden(
                        "You don't have permission to view students");
                }
                // No filter - can see all students
                break;

            default:
                return ServiceResult<IEnumerable<StudentDetailsDto>>.Forbidden(
                    "You don't have permission to view students");
        }

        var students = await query
            .Include(s => s.ApplicationUser)
            .Select(s => new StudentDetailsDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.ApplicationUser != null ? s.ApplicationUser.Email : s.Email,
                Instrument = s.Instrument,
                Bio = s.Bio,
                GPA = s.GPA
            })
            .ToListAsync();

        return ServiceResult<IEnumerable<StudentDetailsDto>>.Success(students);
    }

    /// <summary>
    /// Private helper to check if current user can access a student
    /// </summary>
    private async Task<bool> CanAccessStudentAsync(int studentId)
    {
        var role = await _permissionService.GetCurrentUserRoleAsync();

        switch (role)
        {
            case Roles.Student:
                return await _permissionService.IsStudentOwnerAsync(studentId);

            case Roles.Guardian:
                return await _permissionService.IsGuardianOfStudentAsync(studentId);

            case Roles.Recruiter:
            case Roles.Director:
                return await _permissionService.HasPermissionAsync(Permissions.ViewStudents);

            default:
                return false;
        }
    }
}

/// <summary>
/// Generic service result for operation outcomes
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public ServiceResultType ResultType { get; set; }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            ResultType = ServiceResultType.Success
        };
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.Failure
        };
    }

    public static ServiceResult<T> Forbidden(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.Forbidden
        };
    }

    public static ServiceResult<T> NotFound(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.NotFound
        };
    }
}

public enum ServiceResultType
{
    Success,
    Failure,
    Forbidden,
    NotFound
}