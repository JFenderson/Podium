using Amazon.S3;
using Amazon.S3.Model;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Podium.API.Jobs;
using Podium.Application.Authorization;
using Podium.Application.Interfaces;
using Podium.Application.Services;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;
using Podium.Infrastructure.Hubs;
using Podium.Infrastructure.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

builder.Services.AddSignalR();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddScoped<ExpireContactRequestsJob>();
builder.Services.AddScoped<ExpireScholarshipOffersJob>();
builder.Services.AddScoped<ArchiveInactiveStudentsJob>();
builder.Services.AddScoped<CleanOldAuditLogsJob>();
builder.Services.AddScoped<ProcessTranscodingQueueJob>();
builder.Services.AddScoped<SendEmailNotificationsJob>();

// Configure Swagger with JWT support
#region
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Document Management API",
        Version = "v1",
        Description = "API for Document Management System"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.CustomOperationIds(apiDesc =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        return $"{controllerName}_{actionName}";
    });

    options.UseAllOfToExtendReferenceSchemas();

    // Replace the problematic OpenApiSecurityRequirement block with the correct usage
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});
});
#endregion


// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        // Optional: Ensure it maps the "role" claim correctly
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for the Hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});



// Register application services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<Podium.Application.Interfaces.IAuthService, AuthService>();
builder.Services.AddScoped<IAuthService, Podium.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<IStudentService, Podium.Application.Services.StudentService>();
builder.Services.AddScoped<IStorageService, Podium.Infrastructure.Services.AzureBlobStorageService>();
builder.Services.AddScoped<IAuditService, Podium.Application.Services.AuditService>();
builder.Services.AddScoped<IScholarshipService, ScholarshipService>();
builder.Services.AddScoped<INotificationService, Podium.Infrastructure.Services.NotificationService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IDirectorService, DirectorService>();
builder.Services.AddScoped<IGuardianService, GuardianService>();
// Register Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BandStaffPermissionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SelfAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, GuardianStudentAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ScholarshipApprovalHandler>();
builder.Services.AddScoped<IAuthorizationHandler, StudentResourceAuthorizationHandler>();

// Register Custom Authorization Service
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<Podium.Core.Interfaces.IStorageService, AzureBlobStorageService>();
// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("StudentOnly", policy => policy.RequireRole(Roles.Student));
    options.AddPolicy("GuardianOnly", policy => policy.RequireRole(Roles.Guardian));
    options.AddPolicy("RecruiterOnly", policy => policy.RequireRole(Roles.Recruiter));
    options.AddPolicy("DirectorOnly", policy => policy.RequireRole(Roles.Director));
    options.AddPolicy("BandStaffOnly", policy => policy.RequireRole(Roles.Recruiter, Roles.Director));

    // Permission-based policies
    options.AddPolicy("CanViewStudents", policy =>
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.ViewStudents)));

    options.AddPolicy("CanRateStudents", policy =>
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.RateStudents)));

    options.AddPolicy("CanSendOffers", policy =>
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.SendOffers)));

    options.AddPolicy("CanManageEvents", policy =>
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.ManageEvents)));

    options.AddPolicy("CanManageStaff", policy =>
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.ManageStaff)));

    // Complex policies
    options.AddPolicy("CanApproveScholarships", policy =>
        policy.Requirements.Add(new ScholarshipApprovalRequirement()));

    options.AddPolicy("CanCreateOffer", policy =>
    {
        policy.RequireRole(Roles.Recruiter, Roles.Director);
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.SendOffers));
    });

    options.AddPolicy("AdminAccess", policy =>
    {
        policy.RequireRole(Roles.Director);
        policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.ManageStaff));
    });
});

// Video Storage Service Registration
var storageProvider = builder.Configuration["StorageProvider"];

if (storageProvider == "AWS")
{
    // Register AWS S3 Client
    var awsOptions = builder.Configuration.GetAWSOptions();
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();

    // Register AWS Implementation
    builder.Services.AddScoped<IVideoStorageService, AwsVideoStorageService>();
    Console.WriteLine("Using AWS S3 for video storage.");
}
else
{
    // Register Azure Implementation (Default)
    builder.Services.AddScoped<IVideoStorageService, AzureVideoStorageService>();
    Console.WriteLine("Using Azure Blob Storage for video storage.");
}

// Configure Storage Service (Azure Blob Storage)
var storageConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
var storageContainerName = builder.Configuration["AzureStorage:ContainerName"] ?? "documents";

if (!string.IsNullOrEmpty(storageConnectionString))
{
    try
    {
        builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
        Console.WriteLine("Azure Blob Storage configured successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to configure Azure Blob Storage: {ex.Message}");
        Console.WriteLine("Application will continue without storage service.");
    }
}
else
{
    Console.WriteLine("Azure Storage connection string not configured. Storage service disabled.");
}

var app = builder.Build();

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
    // Authorization = new [] { new MyAuthorizationFilter() } 
});

// We use a scope here to ensure we don't hold onto resources, 
// though RecurringJob.AddOrUpdate typically handles static references.
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

// --- SEEDING CALL HERE ---
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // FIX: Pass the 3 arguments individually
        await DataSeeder.SeedDataAsync(userManager, roleManager, context);
    }
}


app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



// Seed database
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