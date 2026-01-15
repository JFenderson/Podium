using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Podium.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that sanitizes sensitive data from log messages and properties.
/// Filters out passwords, JWT tokens, API keys, credit card numbers, and SSNs.
/// </summary>
public class SensitiveDataFilter : ILogEventEnricher
{
    // Regex patterns for sensitive data
    private static readonly Regex PasswordPattern = new(@"(?i)(password|pwd|pass)\s*[:=]\s*[^\s,;]+", RegexOptions.Compiled);
    private static readonly Regex JwtTokenPattern = new(@"Bearer\s+[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+", RegexOptions.Compiled);
    private static readonly Regex ApiKeyPattern = new(@"(?i)(api[_-]?key|apikey|access[_-]?key|secret[_-]?key)\s*[:=]\s*[^\s,;]+", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    // Sensitive property names to filter
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "Pwd",
        "Pass",
        "Secret",
        "Token",
        "Authorization",
        "ApiKey",
        "AccessKey",
        "SecretKey",
        "ConnectionString",
        "CreditCard",
        "CardNumber",
        "SSN",
        "SocialSecurityNumber"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Filter message template
        if (logEvent.MessageTemplate != null)
        {
            var messageText = logEvent.MessageTemplate.Text;
            if (!string.IsNullOrEmpty(messageText) && ContainsSensitiveData(messageText))
            {
                // We can't modify the message template directly, but we can add a warning
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "SensitiveDataWarning", 
                    "Message may contain sensitive data"));
            }
        }

        // Filter properties
        var propertiesToRemove = new List<string>();
        var propertiesToReplace = new Dictionary<string, LogEventPropertyValue>();

        foreach (var property in logEvent.Properties)
        {
            // Check if property name is sensitive
            if (IsSensitivePropertyName(property.Key))
            {
                propertiesToReplace[property.Key] = new ScalarValue("***REDACTED***");
                continue;
            }

            // Check if property value contains sensitive data
            if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
            {
                var sanitized = SanitizeString(stringValue);
                if (sanitized != stringValue)
                {
                    propertiesToReplace[property.Key] = new ScalarValue(sanitized);
                }
            }
        }

        // Apply replacements
        foreach (var kvp in propertiesToReplace)
        {
            logEvent.RemovePropertyIfPresent(kvp.Key);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(kvp.Key, kvp.Value));
        }
    }

    /// <summary>
    /// Checks if the property name is in the sensitive list.
    /// </summary>
    private static bool IsSensitivePropertyName(string propertyName)
    {
        return SensitivePropertyNames.Contains(propertyName);
    }

    /// <summary>
    /// Checks if a string contains any sensitive data patterns.
    /// </summary>
    private static bool ContainsSensitiveData(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return PasswordPattern.IsMatch(text) ||
               JwtTokenPattern.IsMatch(text) ||
               ApiKeyPattern.IsMatch(text) ||
               CreditCardPattern.IsMatch(text) ||
               SsnPattern.IsMatch(text);
    }

    /// <summary>
    /// Sanitizes a string by replacing sensitive data with redacted placeholders.
    /// </summary>
    private static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;

        // Replace patterns with redacted versions
        result = PasswordPattern.Replace(result, "$1=***REDACTED***");
        result = JwtTokenPattern.Replace(result, "Bearer ***REDACTED_TOKEN***");
        result = ApiKeyPattern.Replace(result, "$1=***REDACTED***");
        result = CreditCardPattern.Replace(result, "****-****-****-****");
        result = SsnPattern.Replace(result, "***-**-****");

        return result;
    }
}
