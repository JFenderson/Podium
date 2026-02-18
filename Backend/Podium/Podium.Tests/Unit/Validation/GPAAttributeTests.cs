using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class GPAAttributeTests
    {
        private GPAAttribute _attribute;

        public GPAAttributeTests()
        {
            _attribute = new GPAAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            decimal? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "GPA" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(2.5)]
        [InlineData(3.5)]
        [InlineData(4.0)]
        public void IsValid_WithValidGPA_ReturnsSuccess(decimal value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "GPA" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(-1.0)]
        [InlineData(4.1)]
        [InlineData(5.0)]
        [InlineData(10.0)]
        public void IsValid_WithInvalidGPA_ReturnsError(decimal value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "GPA" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("between 0.0 and 4.0");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(2.5)]
        [InlineData(3.5)]
        [InlineData(4.0)]
        public void IsValid_WithValidDoubleGPA_ReturnsSuccess(double value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "GPA" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(4.1)]
        public void IsValid_WithInvalidDoubleGPA_ReturnsError(double value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "GPA" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }
    }
}
