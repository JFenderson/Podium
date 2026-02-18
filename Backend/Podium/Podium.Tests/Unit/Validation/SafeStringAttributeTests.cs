using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class SafeStringAttributeTests
    {
        private SafeStringAttribute _attribute;

        public SafeStringAttributeTests()
        {
            _attribute = new SafeStringAttribute();
        }

        [Fact]
        public void IsValid_WithValidString_ReturnsSuccess()
        {
            // Arrange
            var value = "This is a safe string";
            var context = new ValidationContext(new object()) { DisplayName = "TestField" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            string? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "TestField" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithEmptyString_ReturnsSuccess()
        {
            // Arrange
            var value = "";
            var context = new ValidationContext(new object()) { DisplayName = "TestField" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("<img src=x onerror=alert('xss')>")]
        [InlineData("<div onclick='alert(\"xss\")'>Click</div>")]
        [InlineData("javascript:alert('xss')")]
        [InlineData("<b onload=alert('xss')>Test</b>")]
        public void IsValid_WithHtmlOrScript_ReturnsError(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "TestField" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("potentially unsafe content");
        }

        [Theory]
        [InlineData("Normal text without tags")]
        [InlineData("Text with numbers 123 and symbols !@#")]
        [InlineData("Text with quotes 'single' and \"double\"")]
        public void IsValid_WithSafeStrings_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "TestField" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }
    }
}
