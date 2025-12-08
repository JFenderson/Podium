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

   
}