using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class ValidRoleAttributeTests
    {
        private ValidRoleAttribute _attribute;

        public ValidRoleAttributeTests()
        {
            _attribute = new ValidRoleAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            string? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "Role" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("Student")]
        [InlineData("Guardian")]
        [InlineData("BandStaff")]
        [InlineData("Director")]
        [InlineData("Admin")]
        public void IsValid_WithValidRoles_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Role" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("student")]
        [InlineData("GUARDIAN")]
        [InlineData("bandstaff")]
        public void IsValid_WithCaseInsensitiveRoles_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Role" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("InvalidRole")]
        [InlineData("Teacher")]
        [InlineData("Recruiter")]
        [InlineData("Unknown")]
        public void IsValid_WithInvalidRoles_ReturnsError(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Role" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("following roles");
        }
    }
}
