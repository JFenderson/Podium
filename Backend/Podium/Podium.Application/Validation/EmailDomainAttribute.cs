using System.ComponentModel.DataAnnotations;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates that an email address belongs to allowed domains (e.g., .edu for students)
    /// </summary>
    public class EmailDomainAttribute : ValidationAttribute
    {
        public string[]? AllowedDomains { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var email = value.ToString()!;

            // Basic email validation
            if (!email.Contains("@"))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} must be a valid email address."
                );
            }

            // Check domain if specified
            if (AllowedDomains != null && AllowedDomains.Length > 0)
            {
                var emailDomain = email.Split('@').Last().ToLower();
                var isValidDomain = AllowedDomains.Any(domain =>
                    emailDomain.EndsWith("." + domain.ToLower()) || emailDomain == domain.ToLower()
                );

                if (!isValidDomain)
                {
                    return new ValidationResult(
                        ErrorMessage ?? $"The field {validationContext.DisplayName} must use one of the allowed domains: {string.Join(", ", AllowedDomains)}."
                    );
                }
            }

            return ValidationResult.Success;
        }
    }
}
