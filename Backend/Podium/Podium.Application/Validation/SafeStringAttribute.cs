using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Podium.Application.Validation
{
    /// <summary>
    /// Validates that a string does not contain HTML/script tags or potentially malicious content
    /// </summary>
    public class SafeStringAttribute : ValidationAttribute
    {
        private static readonly Regex HtmlTagPattern = new Regex(
            @"<[^>]*>|javascript:|onerror=|onclick=|onload=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var stringValue = value.ToString()!;

            if (HtmlTagPattern.IsMatch(stringValue))
            {
                return new ValidationResult(
                    ErrorMessage ?? $"The field {validationContext.DisplayName} contains potentially unsafe content (HTML tags or scripts)."
                );
            }

            return ValidationResult.Success;
        }
    }
}
