using System.ComponentModel.DataAnnotations;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates GPA is within the range of 0.0 to 4.0
    /// </summary>
    public class GPAAttribute : ValidationAttribute
    {
        private const decimal MinGPA = 0.0m;
        private const decimal MaxGPA = 4.0m;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is decimal decimalValue)
            {
                if (decimalValue < MinGPA || decimalValue > MaxGPA)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"The field {validationContext.DisplayName} must be between {MinGPA} and {MaxGPA}."
                    );
                }
            }
            else if (value is double doubleValue)
            {
                if (doubleValue < (double)MinGPA || doubleValue > (double)MaxGPA)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"The field {validationContext.DisplayName} must be between {MinGPA} and {MaxGPA}."
                    );
                }
            }
            else if (value is float floatValue)
            {
                if (floatValue < (float)MinGPA || floatValue > (float)MaxGPA)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"The field {validationContext.DisplayName} must be between {MinGPA} and {MaxGPA}."
                    );
                }
            }
            else
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be a numeric value."
                );
            }

            return ValidationResult.Success;
        }
    }
}
