using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Podium.API.Extensions;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Telemetry;

namespace Podium.Tests.Unit.Extensions
{
    /// <summary>
    /// Unit tests for MonitoringExtensions to verify conditional telemetry service registration.
    /// </summary>
    public class MonitoringExtensionsTests
    {
        [Fact]
        public void AddPodiumTelemetryServices_WithApplicationInsightsDisabled_RegistersNoOpService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging infrastructure
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ENABLE_APPLICATION_INSIGHTS", "false" }
                })
                .Build();

            // Act
            services.AddPodiumTelemetryServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var telemetryService = serviceProvider.GetService<ITelemetryService>();
            telemetryService.Should().NotBeNull();
            telemetryService.Should().BeOfType<NoOpTelemetryService>();
        }

        [Fact]
        public void AddPodiumTelemetryServices_WithNoConnectionString_RegistersNoOpService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging infrastructure
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            // Act
            services.AddPodiumTelemetryServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var telemetryService = serviceProvider.GetService<ITelemetryService>();
            telemetryService.Should().NotBeNull();
            telemetryService.Should().BeOfType<NoOpTelemetryService>();
        }

        [Fact]
        public void AddPodiumTelemetryServices_WithEmptyConnectionString_RegistersNoOpService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging infrastructure
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "APPLICATIONINSIGHTS_CONNECTION_STRING", "" },
                    { "ENABLE_APPLICATION_INSIGHTS", "true" }
                })
                .Build();

            // Act
            services.AddPodiumTelemetryServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var telemetryService = serviceProvider.GetService<ITelemetryService>();
            telemetryService.Should().NotBeNull();
            telemetryService.Should().BeOfType<NoOpTelemetryService>();
        }

        [Fact]
        public void AddPodiumTelemetryServices_WithApplicationInsightsDisabled_DoesNotRegisterMetricsService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(); // Add logging infrastructure
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ENABLE_APPLICATION_INSIGHTS", "false" }
                })
                .Build();

            // Act
            services.AddPodiumTelemetryServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var metricsService = serviceProvider.GetService<MetricsService>();
            metricsService.Should().BeNull();
        }
    }
}
