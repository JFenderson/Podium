using Hangfire;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Podium.API.HealthChecks;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Logging;
using Podium.Infrastructure.Telemetry;
using Serilog;

namespace Podium.API.Extensions;

/// <summary>
/// Extension methods for configuring monitoring, logging, and observability services.
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds Application Insights telemetry services with custom configuration.
    /// </summary>
    public static IServiceCollection AddPodiumApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] 
            ?? configuration["ApplicationInsights:ConnectionString"];

        var enableAppInsights = configuration.GetValue<bool>("ENABLE_APPLICATION_INSIGHTS", true);

        if (!string.IsNullOrEmpty(connectionString) && enableAppInsights)
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
                options.EnableAdaptiveSampling = true;
                options.EnableQuickPulseMetricStream = true;
            });

            // Add custom telemetry initializer for enriching data
            services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();

            Console.WriteLine("Application Insights configured with connection string");
        }
        else
        {
            Console.WriteLine("Application Insights disabled (no connection string or ENABLE_APPLICATION_INSIGHTS=false)");
        }

        return services;
    }

    /// <summary>
    /// Adds custom telemetry tracking services.
    /// Conditionally registers real or no-op implementations based on Application Insights configuration.
    /// </summary>
    public static IServiceCollection AddPodiumTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] 
            ?? configuration["ApplicationInsights:ConnectionString"];
        var isEnabled = configuration.GetValue<bool>("ENABLE_APPLICATION_INSIGHTS", true);

        if (!string.IsNullOrWhiteSpace(connectionString) && isEnabled)
        {
            // Application Insights is enabled - register real implementations
            services.AddScoped<ITelemetryService, TelemetryService>();
            services.AddScoped<MetricsService>();
            Console.WriteLine("Telemetry services registered (Application Insights enabled)");
        }
        else
        {
            // Application Insights is disabled - register no-op implementation
            services.AddScoped<ITelemetryService, NoOpTelemetryService>();
            Console.WriteLine("No-op telemetry service registered (Application Insights disabled)");
        }

        return services;
    }

    /// <summary>
    /// Adds comprehensive health checks for all application dependencies.
    /// </summary>
    public static IServiceCollection AddPodiumHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: new[] { "db", "sql", "ready" })
            .AddCheck<MemoryHealthCheck>(
                "memory",
                tags: new[] { "memory", "resources" })
            .AddCheck<DiskSpaceHealthCheck>(
                "diskspace",
                tags: new[] { "disk", "resources" });

        // Add blob storage health check if configured
        var storageConnectionString = configuration["AzureStorage:ConnectionString"];
        if (!string.IsNullOrEmpty(storageConnectionString))
        {
            healthChecksBuilder.AddCheck<BlobStorageHealthCheck>(
                "blobstorage",
                tags: new[] { "storage", "azure", "ready" });
        }

        // Add Hangfire health check
        healthChecksBuilder.AddCheck<HangfireHealthCheck>(
            "hangfire",
            tags: new[] { "hangfire", "jobs", "ready" });

        // Add Health Checks UI (optional)
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(configuration.GetValue<int>("HEALTH_CHECK_INTERVAL_SECONDS", 30));
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.AddHealthCheckEndpoint("Podium API", "/health");
        })
        .AddSqlServerStorage(configuration.GetConnectionString("DefaultConnection")!); // Use SQL Server storage

        return services;
    }

    /// <summary>
    /// Configures Serilog with all enrichers and the sensitive data filter.
    /// </summary>
    public static LoggerConfiguration UsePodiumLogging(this LoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration
            .Enrich.With<SensitiveDataFilter>()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithEnvironmentName();
    }

    /// <summary>
    /// Custom telemetry initializer for enriching Application Insights data.
    /// </summary>
    private class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(Microsoft.ApplicationInsights.Channel.ITelemetry telemetry)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Add correlation ID
                if (!string.IsNullOrEmpty(httpContext.TraceIdentifier))
                {
                    telemetry.Context.Properties["RequestId"] = httpContext.TraceIdentifier;
                }

                // Add user information if authenticated
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value 
                        ?? httpContext.User.FindFirst("userId")?.Value
                        ?? httpContext.User.Identity.Name;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        telemetry.Context.User.AuthenticatedUserId = userId;
                        telemetry.Context.Properties["UserId"] = userId;
                    }
                }

                // Add environment name
                telemetry.Context.Properties["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            }

            // Add application version
            telemetry.Context.Component.Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        }
    }
}
