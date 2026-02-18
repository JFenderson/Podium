using Ganss.Xss;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    /// <summary>
    /// Service for sanitizing HTML content to prevent XSS attacks
    /// Uses the HtmlSanitizer library (Ganss.Xss)
    /// </summary>
    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlSanitizerService()
        {
            _sanitizer = new HtmlSanitizer();

            // Configure allowed tags - only basic formatting
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");

            // Remove all attributes to prevent event handlers and inline styles
            _sanitizer.AllowedAttributes.Clear();

            // Remove all CSS to prevent CSS-based attacks
            _sanitizer.AllowedCssProperties.Clear();

            // Remove all schemes (javascript:, data:, etc.)
            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
        }

        /// <summary>
        /// Sanitizes HTML content by removing potentially dangerous tags and attributes
        /// </summary>
        /// <param name="html">The HTML content to sanitize</param>
        /// <returns>Sanitized HTML content</returns>
        public string Sanitize(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            return _sanitizer.Sanitize(html);
        }

        /// <summary>
        /// Sanitizes multiple HTML strings
        /// </summary>
        /// <param name="htmlStrings">Collection of HTML strings to sanitize</param>
        /// <returns>Collection of sanitized HTML strings</returns>
        public IEnumerable<string> SanitizeMany(IEnumerable<string> htmlStrings)
        {
            if (htmlStrings == null)
            {
                return Enumerable.Empty<string>();
            }

            return htmlStrings.Select(Sanitize);
        }
    }
}
