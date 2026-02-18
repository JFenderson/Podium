using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class PhoneNumberAttributeTests
    {
        private PhoneNumberAttribute _attribute;

        public PhoneNumberAttributeTests()
        {
            _attribute = new PhoneNumberAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            string? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "PhoneNumber" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("1234567890")]
        [InlineData("123-456-7890")]
        [InlineData("(123) 456-7890")]
        [InlineData("123.456.7890")]
        [InlineData("123 456 7890")]
        [InlineData("+11234567890")]
        public void IsValid_WithValidPhoneNumbers_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "PhoneNumber" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("abc-def-ghij")]
        [InlineData("12-345-6789")]
        [InlineData("1234567")]
        [InlineData("12345678901234567890")]
        [InlineData("phone number")]
        public void IsValid_WithInvalidPhoneNumbers_ReturnsError(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "PhoneNumber" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("valid US phone number format");
        }
    }
}
