using System.ComponentModel.DataAnnotations;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates that graduation year is within a reasonable range (current year to current year + 4)
    /// </summary>
    public class GraduationYearAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is not int year)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be a valid year."
                );
            }

            var currentYear = DateTime.Now.Year;
            var minYear = currentYear;
            var maxYear = currentYear + 4;

            if (year < minYear || year > maxYear)
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be between {minYear} and {maxYear}."
                );
            }

            return ValidationResult.Success;
        }
    }
}
