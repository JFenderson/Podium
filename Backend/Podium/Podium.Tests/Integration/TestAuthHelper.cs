using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Podium.Tests.Integration
{
    public static class TestAuthHelper
    {
        public static async Task<HttpClient> GetAuthenticatedClient(HttpClient client, string email, string password)
        {
            // 1. Prepare Login Data
            var loginData = new { Email = email, Password = password };

            // 2. Call the Login Endpoint
            var response = await client.PostAsJsonAsync("/api/auth/login", loginData);
            var content = await response.Content.ReadAsStringAsync();

            // 1. Check if the server actually rejected the login
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Login Failed. Status: {response.StatusCode}. Response: {content}");
            }

            // 2. Parse JSON safely (Case-insensitive check)
            var json = JsonNode.Parse(content);

            
            var token = json?["accessToken"]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                // This will print the JSON to your terminal so we can see what went wrong
                throw new Exception($"Login succeeded (200 OK) but 'accessToken' property was missing. Actual JSON: {content}");
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }
}