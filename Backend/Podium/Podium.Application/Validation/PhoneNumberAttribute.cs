using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates US phone number formats: (123) 456-7890, 123-456-7890, 1234567890, etc.
    /// </summary>
    public class PhoneNumberAttribute : ValidationAttribute
    {
        private static readonly Regex PhonePattern = new Regex(
            @"^(\+1)?[-\s\.]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4}$",
            RegexOptions.Compiled
        );

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var phoneNumber = value.ToString()!;

            if (!PhonePattern.IsMatch(phoneNumber))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be a valid US phone number format (e.g., (123) 456-7890, 123-456-7890, or 1234567890)."
                );
            }

            return ValidationResult.Success;
        }
    }
}
