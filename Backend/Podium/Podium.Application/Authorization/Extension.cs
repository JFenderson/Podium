using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Authorization
{
    internal class Extension
    {
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BandRecruitment.Authorization;

/// <summary>
/// Extension methods for accessing user information in controllers
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    public static int? GetCurrentUserId(this ControllerBase controller)
    {
        var userIdClaim = controller.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Get current user role from claims
    /// </summary>
    public static string? GetCurrentUserRole(this ControllerBase controller)
    {
        return controller.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Check if current user has a specific role
    /// </summary>
    public static bool IsInRole(this ControllerBase controller, string role)
    {
        var userRole = controller.GetCurrentUserRole();
        return userRole == role;
    }

    /// <summary>
    /// Check if current user is a Student
    /// </summary>
    public static bool IsStudent(this ControllerBase controller)
    {
        return controller.IsInRole(Roles.Student);
    }

    /// <summary>
    /// Check if current user is a Guardian
    /// </summary>
    public static bool IsGuardian(this ControllerBase controller)
    {
        return controller.IsInRole(Roles.Guardian);
    }

    /// <summary>
    /// Check if current user is a Recruiter
    /// </summary>
    public static bool IsRecruiter(this ControllerBase controller)
    {
        return controller.IsInRole(Roles.Recruiter);
    }

    /// <summary>
    /// Check if current user is a Director
    /// </summary>
    public static bool IsDirector(this ControllerBase controller)
    {
        return controller.IsInRole(Roles.Director);
    }

    /// <summary>
    /// Check if current user is any BandStaff (Recruiter or Director)
    /// </summary>
    public static bool IsBandStaff(this ControllerBase controller)
    {
        var role = controller.GetCurrentUserRole();
        return role == Roles.Recruiter || role == Roles.Director;
    }

    /// <summary>
    /// Get current user's email from claims
    /// </summary>
    public static string? GetCurrentUserEmail(this ControllerBase controller)
    {
        return controller.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Get current user's name from claims
    /// </summary>
    public static string? GetCurrentUserName(this ControllerBase controller)
    {
        return controller.User?.FindFirst(ClaimTypes.Name)?.Value;
    }
}

/// <summary>
/// Helper methods for authorization checks in business logic or services
/// </summary>
public static class AuthorizationHelpers
{
    /// <summary>
    /// Check if a user ID belongs to a specific student
    /// </summary>
    public static bool IsStudentOwner(int userId, int studentUserId)
    {
        return userId == studentUserId;
    }

    /// <summary>
    /// Validate that a user can only access their own resource
    /// </summary>
    public static bool CanAccessOwnResource(int currentUserId, int resourceUserId)
    {
        return currentUserId == resourceUserId;
    }

    /// <summary>
    /// Check if all required permissions are present
    /// </summary>
    public static bool HasAllPermissions(BandStaffPermissions permissions, params string[] requiredPermissions)
    {
        foreach (var permission in requiredPermissions)
        {
            var hasPermission = permission switch
            {
                Permissions.ViewStudents => permissions.CanViewStudents,
                Permissions.RateStudents => permissions.CanRateStudents,
                Permissions.SendOffers => permissions.CanSendOffers,
                Permissions.ManageEvents => permissions.CanManageEvents,
                Permissions.ManageStaff => permissions.CanManageStaff,
                _ => false
            };

            if (!hasPermission)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check if any of the required permissions are present
    /// </summary>
    public static bool HasAnyPermission(BandStaffPermissions permissions, params string[] requiredPermissions)
    {
        foreach (var permission in requiredPermissions)
        {
            var hasPermission = permission switch
            {
                Permissions.ViewStudents => permissions.CanViewStudents,
                Permissions.RateStudents => permissions.CanRateStudents,
                Permissions.SendOffers => permissions.CanSendOffers,
                Permissions.ManageEvents => permissions.CanManageEvents,
                Permissions.ManageStaff => permissions.CanManageStaff,
                _ => false
            };

            if (hasPermission)
            {
                return true;
            }
        }
        return false;
    }
}