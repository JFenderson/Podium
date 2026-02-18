using FluentAssertions;
using Podium.Infrastructure.Services;
using Xunit;

namespace Podium.Tests.Unit.Services
{
    public class HtmlSanitizerServiceTests
    {
        private readonly HtmlSanitizerService _service;

        public HtmlSanitizerServiceTests()
        {
            _service = new HtmlSanitizerService();
        }

        [Fact]
        public void Sanitize_WithNull_ReturnsEmptyString()
        {
            // Arrange
            string? input = null;

            // Act
            var result = _service.Sanitize(input!);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Sanitize_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var input = "";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Sanitize_WithPlainText_ReturnsUnchanged()
        {
            // Arrange
            var input = "This is plain text";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().Be(input);
        }

        [Fact]
        public void Sanitize_WithAllowedTags_PreservesTags()
        {
            // Arrange
            var input = "<b>Bold</b> and <i>italic</i> text";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().Contain("<b>").And.Contain("</b>");
            result.Should().Contain("<i>").And.Contain("</i>");
        }

        [Fact]
        public void Sanitize_WithScriptTag_RemovesScript()
        {
            // Arrange
            var input = "<script>alert('xss')</script>Safe text";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("<script>");
            result.Should().NotContain("</script>");
            result.Should().NotContain("alert");
        }

        [Fact]
        public void Sanitize_WithOnClickAttribute_RemovesAttribute()
        {
            // Arrange
            var input = "<div onclick='alert(\"xss\")'>Click me</div>";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("onclick");
            result.Should().NotContain("alert");
        }

        [Fact]
        public void Sanitize_WithIframeTag_RemovesIframe()
        {
            // Arrange
            var input = "<iframe src='malicious.com'></iframe>";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("<iframe>");
            result.Should().NotContain("</iframe>");
        }

        [Theory]
        [InlineData("<img src=x onerror=alert('xss')>")]
        [InlineData("<svg onload=alert('xss')>")]
        [InlineData("<body onload=alert('xss')>")]
        public void Sanitize_WithEventHandlers_RemovesEventHandlers(string input)
        {
            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("alert");
            result.Should().NotContain("onerror");
            result.Should().NotContain("onload");
        }

        [Fact]
        public void Sanitize_WithStyleAttribute_RemovesStyle()
        {
            // Arrange
            var input = "<p style='color: red;'>Red text</p>";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("style");
            result.Should().Contain("Red text");
        }

        [Fact]
        public void Sanitize_WithJavascriptScheme_RemovesJavascript()
        {
            // Arrange
            var input = "<a href='javascript:alert(\"xss\")'>Click</a>";

            // Act
            var result = _service.Sanitize(input);

            // Assert
            result.Should().NotContain("javascript:");
            result.Should().NotContain("alert");
        }

        [Fact]
        public void SanitizeMany_WithMultipleStrings_SanitizesAll()
        {
            // Arrange
            var inputs = new[]
            {
                "Safe text",
                "<script>alert('xss')</script>",
                "<b>Bold</b> text"
            };

            // Act
            var results = _service.SanitizeMany(inputs).ToList();

            // Assert
            results.Should().HaveCount(3);
            results[0].Should().Be("Safe text");
            results[1].Should().NotContain("<script>");
            results[2].Should().Contain("<b>");
        }

        [Fact]
        public void SanitizeMany_WithNull_ReturnsEmptyCollection()
        {
            // Arrange
            IEnumerable<string>? inputs = null;

            // Act
            var results = _service.SanitizeMany(inputs!);

            // Assert
            results.Should().NotBeNull();
            results.Should().BeEmpty();
        }

        [Fact]
        public void SanitizeMany_WithEmptyCollection_ReturnsEmptyCollection()
        {
            // Arrange
            var inputs = Enumerable.Empty<string>();

            // Act
            var results = _service.SanitizeMany(inputs);

            // Assert
            results.Should().NotBeNull();
            results.Should().BeEmpty();
        }
    }
}
