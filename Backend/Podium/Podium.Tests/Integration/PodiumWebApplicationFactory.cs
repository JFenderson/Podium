using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Podium.Tests.Integration
{
    // This custom factory overrides the default startup behavior
    public class PodiumWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Force the environment to "Development".
            // This triggers the logic in your Program.cs to run the DataSeeder automatically.
            builder.UseEnvironment("Development");
        }
    }
}