using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Podium.Infrastructure.Telemetry;

namespace Podium.Tests.Unit.Telemetry
{
    /// <summary>
    /// Unit tests for NoOpTelemetryService to ensure it doesn't throw exceptions
    /// and properly logs debug messages when Application Insights is disabled.
    /// </summary>
    public class NoOpTelemetryServiceTests
    {
        private readonly Mock<ILogger<NoOpTelemetryService>> _mockLogger;
        private readonly NoOpTelemetryService _service;

        public NoOpTelemetryServiceTests()
        {
            _mockLogger = new Mock<ILogger<NoOpTelemetryService>>();
            _service = new NoOpTelemetryService(_mockLogger.Object);
        }

        [Fact]
        public void TrackAuthenticationAttempt_DoesNotThrowException()
        {
            // Arrange
            var userId = "test-user";
            var success = true;
            var reason = "Valid credentials";

            // Act
            var action = () => _service.TrackAuthenticationAttempt(userId, success, reason);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void TrackVideoUpload_DoesNotThrowException()
        {
            // Arrange
            var userId = "test-user";
            var fileSizeBytes = 1024L;
            var duration = TimeSpan.FromSeconds(30);
            var success = true;

            // Act
            var action = () => _service.TrackVideoUpload(userId, fileSizeBytes, duration, success);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void TrackDatabaseQuery_DoesNotThrowException()
        {
            // Arrange
            var queryName = "GetStudentById";
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            var action = () => _service.TrackDatabaseQuery(queryName, duration);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void TrackApiEndpoint_DoesNotThrowException()
        {
            // Arrange
            var endpoint = "/api/students";
            var responseTime = TimeSpan.FromMilliseconds(100);
            var statusCode = 200;

            // Act
            var action = () => _service.TrackApiEndpoint(endpoint, responseTime, statusCode);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void TrackException_DoesNotThrowException()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var properties = new Dictionary<string, string> { { "Key", "Value" } };

            // Act
            var action = () => _service.TrackException(exception, properties);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void TrackAuthenticationAttempt_LogsDebugMessage()
        {
            // Arrange
            var userId = "test-user";
            var success = true;
            var reason = "Valid credentials";

            // Act
            _service.TrackAuthenticationAttempt(userId, success, reason);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Application Insights disabled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
