using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using System.Net;

namespace Podium.Tests.Integration
{
    public class GuardianTests : IClassFixture<PodiumWebApplicationFactory<Program>>
    {
        private readonly PodiumWebApplicationFactory<Program> _factory;

        public GuardianTests(PodiumWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Dashboard_Returns_Data_For_Admin()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Login as the Admin user (created by your Seeder)
            client = await TestAuthHelper.GetAuthenticatedClient(client, "mom@gmail.com", "Password123!");

            // Act
            // Note: Admin has the "Guardian" role in your seeder? 
            // If not, this might return 403 Forbidden if the endpoint requires "Guardian" role specifically.
            // Let's test if we can hit it.
            var response = await client.GetAsync("/api/Guardian/dashboard");


            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("totalUnreadNotifications");
            content.Should().Contain("linkedStudents");
        }
    }
}