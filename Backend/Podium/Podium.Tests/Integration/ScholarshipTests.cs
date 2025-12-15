using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Podium.Tests.Integration
{
    public class ScholarshipTests : IClassFixture<PodiumWebApplicationFactory<Program>>
    {
        private readonly PodiumWebApplicationFactory<Program> _factory;

        public ScholarshipTests(PodiumWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Full_Scholarship_Lifecycle_DirectorSend_StudentAccept()
        {
            var client = _factory.CreateClient();

            // ============================================================
            // STEP 1: Director Sends an Offer
            // ============================================================

            // 1a. Login as ASU Director
            client = await TestAuthHelper.GetAuthenticatedClient(client, "director@asu.edu", "Password123!");

            // 1b. Find the Student's ID (We know 'student@gmail.com' exists from Seeder)
            // We'll search for them to get their internal ID
            var searchResponse = await client.GetAsync("/api/Director/students?query=Jordan");
            searchResponse.EnsureSuccessStatusCode();
            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            var searchJson = JsonNode.Parse(searchContent);

            // Assuming the search returns a list of students, grab the first one's ID
            var studentIdNode = searchJson?["students"]?[0]?["id"] ?? searchJson?[0]?["id"];
            var studentId = int.Parse(studentIdNode!.ToString());

            // 1c. Create the Offer Payload
            var offerPayload = new
            {
                StudentId = studentId,
                Amount = 15000,
                ScholarshipType = "Full Ride",
                Description = "We want you for Tuba!",
                Requirements = "Must maintain 3.0 GPA",
                Deadline = DateTime.UtcNow.AddDays(30)
            };

            // 1d. Send Offer
            var createResponse = await client.PostAsJsonAsync("/api/ScholarshipOffers", offerPayload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // 1e. Extract Offer ID
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var offerNode = JsonNode.Parse(createContent);
            var offerId = int.Parse(offerNode?["id"]?.ToString() ?? throw new Exception("Offer ID not found"));

            // ============================================================
            // STEP 2: Student Accepts the Offer
            // ============================================================

            // 2a. Login as Student
            client = await TestAuthHelper.GetAuthenticatedClient(client, "student@gmail.com", "Password123!");

            // 2b. Verify Student can see the offer
            var offersResponse = await client.GetAsync("/api/Student/scholarships");
            offersResponse.EnsureSuccessStatusCode();
            var offersContent = await offersResponse.Content.ReadAsStringAsync();
            offersContent.Should().Contain("We want you for Tuba!");

            // 2c. Accept the Offer
            var acceptPayload = new { IsAccepted = true, Notes = "I am excited to join!" };
            // Note: Adjust the endpoint URL if your specific route is different (e.g. /api/ScholarshipOffers/{id}/respond)
            var acceptResponse = await client.PutAsJsonAsync($"/api/ScholarshipOffers/{offerId}/respond", acceptPayload);

            // Assert Success
            acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2d. Verify status is now "Accepted"
            var finalCheck = await client.GetAsync($"/api/ScholarshipOffers/{offerId}");
            var finalContent = await finalCheck.Content.ReadAsStringAsync();
            finalContent.Should().Contain("Accepted");
        }
    }
}