using Microsoft.AspNetCore.Authorization;

namespace Podium.Infrastructure.Authorization
{
    /// <summary>
    /// Requirement for role-based authorization
    /// </summary>
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string Role { get; }

        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    /// <summary>
    /// Requirement for BandStaff permission checks
    /// </summary>
    public class BandStaffPermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public BandStaffPermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    /// <summary>
    /// Requirement for accessing own resources only
    /// </summary>
    public class SelfAccessRequirement : IAuthorizationRequirement
    {
        public string ResourceIdClaimType { get; }

        public SelfAccessRequirement(string resourceIdClaimType = "sub")
        {
            ResourceIdClaimType = resourceIdClaimType;
        }
    }

    /// <summary>
    /// Requirement for guardian accessing linked students
    /// </summary>
    public class GuardianStudentAccessRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Requirement for scholarship offer approval (Directors only)
    /// </summary>
    public class ScholarshipApprovalRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Resource-based authorization requirement for accessing specific entities
    /// </summary>
    public class ResourceAccessRequirement : IAuthorizationRequirement
    {
        public string Operation { get; }

        public ResourceAccessRequirement(string operation)
        {
            Operation = operation;
        }
    }

    /// <summary>
    /// Constants for authorization operations
    /// </summary>
    public static class Operations
    {
        public const string Create = "Create";
        public const string Read = "Read";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Approve = "Approve";
    }

    /// <summary>
    /// Constants for permission types
    /// </summary>
    public static class Permissions
    {
        public const string ViewStudents = "ViewStudents";
        public const string RateStudents = "RateStudents";
        public const string SendOffers = "SendOffers";
        public const string ManageEvents = "ManageEvents";
        public const string ManageStaff = "ManageStaff";
    }

    /// <summary>
    /// Constants for role names
    /// </summary>
    public static class Roles
    {
        public const string Student = "Student";
        public const string Guardian = "Guardian";
        public const string Recruiter = "Recruiter";
        public const string Director = "Director";
        public const string BandStaff = "BandStaff"; // Generic role for any staff
    }
}