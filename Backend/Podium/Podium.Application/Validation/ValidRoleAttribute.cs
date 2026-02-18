using System.ComponentModel.DataAnnotations;
using Podium.Core.Constants;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates that a role is one of the allowed application roles
    /// </summary>
    public class ValidRoleAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var role = value.ToString()!;
            var allowedRoles = Roles.GetAllRoles();

            if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be one of the following roles: {string.Join(", ", allowedRoles)}."
                );
            }

            return ValidationResult.Success;
        }
    }
}
