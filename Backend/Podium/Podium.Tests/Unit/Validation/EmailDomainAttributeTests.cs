using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class EmailDomainAttributeTests
    {
        [Fact]
        public void IsValid_WithValidEmailNoDomainRestriction_ReturnsSuccess()
        {
            // Arrange
            var attribute = new EmailDomainAttribute();
            var value = "test@example.com";
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            var attribute = new EmailDomainAttribute();
            string? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithValidEduDomain_ReturnsSuccess()
        {
            // Arrange
            var attribute = new EmailDomainAttribute { AllowedDomains = new[] { "edu" } };
            var value = "student@university.edu";
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithInvalidDomain_ReturnsError()
        {
            // Arrange
            var attribute = new EmailDomainAttribute { AllowedDomains = new[] { "edu" } };
            var value = "student@gmail.com";
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("allowed domains");
        }

        [Theory]
        [InlineData("student@mit.edu")]
        [InlineData("faculty@stanford.edu")]
        [InlineData("admin@harvard.edu")]
        public void IsValid_WithValidEduDomains_ReturnsSuccess(string value)
        {
            // Arrange
            var attribute = new EmailDomainAttribute { AllowedDomains = new[] { "edu" } };
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("student@gmail.com")]
        [InlineData("user@yahoo.com")]
        [InlineData("test@outlook.com")]
        public void IsValid_WithNonEduDomains_ReturnsError(string value)
        {
            // Arrange
            var attribute = new EmailDomainAttribute { AllowedDomains = new[] { "edu" } };
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@@")]
        [InlineData("@test")]
        [InlineData("test@")]
        [InlineData("test@@example.com")]
        public void IsValid_WithInvalidEmailFormat_ReturnsError(string value)
        {
            // Arrange
            var attribute = new EmailDomainAttribute();
            var context = new ValidationContext(new object()) { DisplayName = "Email" };

            // Act
            var result = attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("valid email address");
        }
    }
}
