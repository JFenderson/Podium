using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Podium.Application.Validation;
using Xunit;

namespace Podium.Tests.Unit.Validation
{
    public class InstrumentAttributeTests
    {
        private InstrumentAttribute _attribute;

        public InstrumentAttributeTests()
        {
            _attribute = new InstrumentAttribute();
        }

        [Fact]
        public void IsValid_WithNull_ReturnsSuccess()
        {
            // Arrange
            string? value = null;
            var context = new ValidationContext(new object()) { DisplayName = "Instrument" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("Flute")]
        [InlineData("Trumpet")]
        [InlineData("Saxophone")]
        [InlineData("Trombone")]
        [InlineData("Clarinet")]
        [InlineData("Percussion")]
        [InlineData("Tuba")]
        [InlineData("French Horn")]
        public void IsValid_WithValidInstruments_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Instrument" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("flute")]
        [InlineData("TRUMPET")]
        [InlineData("saxophone")]
        public void IsValid_WithCaseInsensitiveInstruments_ReturnsSuccess(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Instrument" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("Guitar Hero")]
        [InlineData("Banjo")]
        [InlineData("Accordion")]
        [InlineData("Invalid Instrument")]
        public void IsValid_WithInvalidInstruments_ReturnsError(string value)
        {
            // Arrange
            var context = new ValidationContext(new object()) { DisplayName = "Instrument" };

            // Act
            var result = _attribute.GetValidationResult(value, context);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result!.ErrorMessage.Should().Contain("valid instrument");
        }

        [Fact]
        public void GetAllowedInstruments_ReturnsNonEmptyList()
        {
            // Act
            var instruments = InstrumentAttribute.GetAllowedInstruments();

            // Assert
            instruments.Should().NotBeNull();
            instruments.Should().NotBeEmpty();
            instruments.Should().Contain("Flute");
            instruments.Should().Contain("Trumpet");
            instruments.Should().Contain("Percussion");
        }
    }
}
