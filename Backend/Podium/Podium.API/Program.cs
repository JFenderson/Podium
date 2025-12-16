using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.API.Extensions;
using Podium.API.Jobs;
using Podium.Core.Entities;
using Podium.Infrastructure.Data;
using Podium.Infrastructure.Hubs;
using Podium.API.Extensions;
using Podium.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------
// REFACTORED: Custom Extension Methods
// ---------------------------------------------------------
builder.Services.AddPodiumSwagger();
builder.Services.AddPodiumIdentity(builder.Configuration);
builder.Services.AddPodiumCoreServices(builder.Configuration, builder.Environment);
// ---------------------------------------------------------

var app = builder.Build();

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
        var context = services.GetRequiredService<ApplicationDbContext>();
        // This ensures the DB has the latest columns (RowVersion, BandBudget)
        await context.Database.MigrateAsync();
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

// Configure Scheduled Jobs
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
}

app.MapHub<NotificationHub>("/notificationHub");
app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply Migrations
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

app.Run();

public partial class Program { }