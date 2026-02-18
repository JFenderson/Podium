namespace Podium.Core.Interfaces
{
    /// <summary>
    /// Service for sanitizing HTML content to prevent XSS attacks
    /// </summary>
    public interface IHtmlSanitizerService
    {
        /// <summary>
        /// Sanitizes HTML content by removing potentially dangerous tags and attributes
        /// </summary>
        /// <param name="html">The HTML content to sanitize</param>
        /// <returns>Sanitized HTML content</returns>
        string Sanitize(string html);

        /// <summary>
        /// Sanitizes multiple HTML strings
        /// </summary>
        /// <param name="htmlStrings">Collection of HTML strings to sanitize</param>
        /// <returns>Collection of sanitized HTML strings</returns>
        IEnumerable<string> SanitizeMany(IEnumerable<string> htmlStrings);
    }
}
