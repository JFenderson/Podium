using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Podium.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Podium.Tests.Integration
{
    // This custom factory overrides the default startup behavior
    public class PodiumWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Use test environment
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override any configuration needed for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                    ["Jwt:Secret"] = "ThisIsATestSecretKeyForJwtTokenGeneration123456789",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext configuration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("PodiumTestDb_" + Guid.NewGuid().ToString());
                });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            // Seed the database
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                
                // You can add test data seeding here if needed
                // DataSeeder.SeedDataAsync(...).Wait();
            }

            return host;
        }
    }
}