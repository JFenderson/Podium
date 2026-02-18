using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class GraduationYearAttributeTests
    {
        private GraduationYearAttribute _attribute;

        public GraduationYearAttributeTests()
        {
            _attribute = new GraduationYearAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            int? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithCurrentYear_ReturnsSuccess()
        {
            // Arrange
            var value = DateTime.Now.Year;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithCurrentYearPlusFour_ReturnsSuccess()
        {
            // Arrange
            var value = DateTime.Now.Year + 4;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithCurrentYearPlusTwo_ReturnsSuccess()
        {
            // Arrange
            var value = DateTime.Now.Year + 2;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithPastYear_ReturnsError()
        {
            // Arrange
            var value = DateTime.Now.Year - 1;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("between");
        }

        [Fact]
        public void IsValid_WithTooFarInFuture_ReturnsError()
        {
            // Arrange
            var value = DateTime.Now.Year + 5;
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("between");
        }

        [Fact]
        public void IsValid_WithStringValue_ReturnsError()
        {
            // Arrange
            var value = "2025";
            var context = new ValidationContext(new object()) { DisplayName = "GraduationYear" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("valid year");
        }
    }
}
