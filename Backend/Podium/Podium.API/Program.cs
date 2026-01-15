using AspNetCoreRateLimit;
using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.API.Extensions;
using Podium.API.Jobs;
using Podium.API.Middleware;
using Podium.Core.Entities;
using Podium.Infrastructure.BackgroundJobs;
using Podium.Infrastructure.Data;
using Podium.Infrastructure.Hubs;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .AddEnvironmentVariables()
        .Build())
    .UsePodiumLogging() // Apply Podium logging configuration with enrichers
    .Enrich.WithProperty("Application", "Podium.API")
    .CreateLogger();

try
{
    Log.Information("Starting Podium API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    // Configure Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Configure CORS with environment variable support
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:4200", "https://localhost:4200" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // ---------------------------------------------------------
    // REFACTORED: Custom Extension Methods
    // ---------------------------------------------------------
    builder.Services.AddPodiumSwagger();
    builder.Services.AddPodiumIdentity(builder.Configuration);
    builder.Services.AddPodiumCoreServices(builder.Configuration, builder.Environment);
    
    // ---------------------------------------------------------
    // MONITORING: Application Insights, Health Checks, Telemetry
    // ---------------------------------------------------------
    builder.Services.AddPodiumApplicationInsights(builder.Configuration);
    builder.Services.AddPodiumTelemetryServices(builder.Configuration);
    builder.Services.AddPodiumHealthChecks(builder.Configuration);
    // ---------------------------------------------------------

    var app = builder.Build();

    // Use custom request logging middleware (adds correlation IDs)
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    // Use performance tracking middleware
    app.UseMiddleware<PerformanceTrackingMiddleware>();

    // Use Security Headers
    app.UseSecurityHeaders();

    // Use IP Rate Limiting
    app.UseIpRateLimiting();

    //Register Global Exception Middleware
    app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Management API V1");
    });
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // Optional: Add authorization filter to restrict dashboard access to Admins
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Skip database migrations in Testing environment
        if (!app.Environment.IsEnvironment("Testing"))
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            // This ensures the DB has the latest columns (RowVersion, BandBudget)
            await context.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DataSeeder.SeedDataAsync(userManager, roleManager, context);
    }
}

// Configure Scheduled Jobs - Skip in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Daily Jobs
    recurringJobManager.AddOrUpdate<ExpireContactRequestsJob>(
        "expire-contact-requests",
        job => job.ExecuteAsync(),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<ExpireScholarshipOffersJob>(
        "expire-scholarships",
        job => job.ExecuteAsync(),
        Cron.Daily);

    recurringJobManager.AddOrUpdate<ArchiveInactiveStudentsJob>(
        "archive-students",
        job => job.ExecuteAsync(),
        Cron.Daily);

    // Weekly Job
    recurringJobManager.AddOrUpdate<CleanOldAuditLogsJob>(
        "clean-audit-logs",
        job => job.ExecuteAsync(),
        Cron.Weekly);

    // High Frequency Job (Every 5 minutes)
    recurringJobManager.AddOrUpdate<ProcessTranscodingQueueJob>(
        "process-transcoding",
        job => job.ExecuteAsync(),
        "*/5 * * * *");

    // Hourly Job - Search Alerts
    recurringJobManager.AddOrUpdate<SearchAlertJob>(
        "process-search-alerts",
        job => job.ProcessSearchAlerts(),
        Cron.Hourly);
}
}

app.MapHub<NotificationHub>("/notificationHub");
app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoints for container orchestration and monitoring
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Health Checks UI endpoint (optional, for dashboard)
app.MapHealthChecksUI(options => options.UIPath = "/healthchecks-ui");

// Apply Migrations - Skip in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Podium API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }