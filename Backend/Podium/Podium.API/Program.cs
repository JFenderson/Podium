using AspNetCoreRateLimit;
using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.API.Extensions;
using Podium.API.Filters;
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

    // CORS is configured in AddPodiumCoreServices (ServiceExtensions.cs)

    // ---------------------------------------------------------
    // REFACTORED: Custom Extension Methods
    // ---------------------------------------------------------
    builder.Services.AddPodiumSwagger();
    builder.Services.AddPodiumIdentity(builder.Configuration, builder.Environment);
    builder.Services.AddPodiumCoreServices(builder.Configuration, builder.Environment);
    
    // ---------------------------------------------------------
    // MONITORING: Application Insights, Health Checks, Telemetry
    // ---------------------------------------------------------
    builder.Services.AddPodiumApplicationInsights(builder.Configuration);
    builder.Services.AddPodiumTelemetryServices(builder.Configuration);
    builder.Services.AddPodiumHealthChecks(builder.Configuration);
    // ---------------------------------------------------------

    // Validate required configuration on startup (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
        ValidateRequiredConfiguration(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // Use custom request logging middleware (adds correlation IDs)
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    // Use performance tracking middleware
    //app.UseMiddleware<PerformanceTrackingMiddleware>();

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
    Authorization = new[] { new HangfireDashboardAuthFilter() }
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

static void ValidateRequiredConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    var errors = new List<string>();

    // JWT
    if (string.IsNullOrWhiteSpace(configuration["JWT:Secret"]))
        errors.Add("JWT:Secret is required.");
    if (string.IsNullOrWhiteSpace(configuration["JWT:Issuer"]))
        errors.Add("JWT:Issuer is required.");
    if (string.IsNullOrWhiteSpace(configuration["JWT:Audience"]))
        errors.Add("JWT:Audience is required.");

    // Database
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        errors.Add("ConnectionStrings:DefaultConnection is required.");

    // Email (only required in non-Development environments)
    if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuration["SendGrid:ApiKey"]))
        errors.Add("SendGrid:ApiKey is required in non-Development environments.");

    // Storage (Cloudflare R2)
    if (!environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(configuration["CloudflareR2:AccountId"]))
            errors.Add("CloudflareR2:AccountId is required in non-Development environments.");
        if (string.IsNullOrWhiteSpace(configuration["CloudflareR2:AccessKeyId"]))
            errors.Add("CloudflareR2:AccessKeyId is required in non-Development environments.");
        if (string.IsNullOrWhiteSpace(configuration["CloudflareR2:SecretAccessKey"]))
            errors.Add("CloudflareR2:SecretAccessKey is required in non-Development environments.");
        if (string.IsNullOrWhiteSpace(configuration["CloudflareR2:BucketName"]))
            errors.Add("CloudflareR2:BucketName is required in non-Development environments.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException(
            $"Application configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"))}");
    }
}

public partial class Program { }