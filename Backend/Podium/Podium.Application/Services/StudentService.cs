using Podium.Infrastructure.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Services
{
   
}
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BandRecruitment.Services;

/// <summary>
/// Example service demonstrating how to use authorization in business logic
/// </summary>
public interface IStudentService
{
    Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId);
    Task<ServiceResult<bool>> UpdateStudentProfileAsync(int studentId, UpdateStudentDto dto);
    Task<ServiceResult<IEnumerable<StudentDetailsDto>>> GetAccessibleStudentsAsync();
}

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authService;

    public StudentService(
        ApplicationDbContext context,
        IAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Get student details with authorization checks
    /// </summary>
    public async Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId)
    {
        var student = await _context.Students
            .Include(s => s.User)
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
            Email = student.User?.Email ?? string.Empty,
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
        var isOwner = await _authService.IsStudentOwnerAsync(studentId);
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
        var role = await _authService.GetCurrentUserRoleAsync();
        var userId = await _authService.GetCurrentUserIdAsync();

        if (userId == null)
        {
            return ServiceResult<IEnumerable<StudentDetailsDto>>.Failure("User not authenticated");
        }

        IQueryable<Student> query = _context.Students;

        switch (role)
        {
            case Roles.Student:
                // Students can only see themselves
                query = query.Where(s => s.UserId == userId.Value);
                break;

            case Roles.Guardian:
                // Guardians can see their linked students
                var guardian = await _context.Guardians
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.UserId == userId.Value);

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
                if (!await _authService.HasPermissionAsync(Permissions.ViewStudents))
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
            .Include(s => s.User)
            .Select(s => new StudentDetailsDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.User.Email,
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
        var role = await _authService.GetCurrentUserRoleAsync();

        switch (role)
        {
            case Roles.Student:
                return await _authService.IsStudentOwnerAsync(studentId);

            case Roles.Guardian:
                return await _authService.IsGuardianOfStudentAsync(studentId);

            case Roles.Recruiter:
            case Roles.Director:
                return await _authService.HasPermissionAsync(Permissions.ViewStudents);

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

public class StudentDetailsDto
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Instrument { get; set; }
    public string? Bio { get; set; }
    public decimal? GPA { get; set; }
}