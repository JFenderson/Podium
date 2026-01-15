using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace Podium.Tests.Integration
{
    // This tells xUnit to spin up your API in memory
    public class HealthCheckTests : IClassFixture<PodiumWebApplicationFactory<Program>>
    {
        private readonly PodiumWebApplicationFactory<Program> _factory;

        public HealthCheckTests(PodiumWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Health_Returns_Success()
        {
            // Arrange: Create a client that talks to the in-memory API
            var client = _factory.CreateClient();

            // Act: Hit the health endpoint
            var response = await client.GetAsync("/health");

            // Assert: Verify we got a 200 OK
            response.EnsureSuccessStatusCode();
            response.StatusCode.ToString().Should().Be("OK");
        }
    }
}