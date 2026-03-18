using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
using Podium.Infrastructure.BackgroundJobs;
using Podium.Infrastructure.Data;
using Podium.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

namespace Podium.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddPodiumSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
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

            return services;
        }

        public static IServiceCollection AddPodiumIdentity(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Configure Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Configure Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
            var jwtSecret = configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Register Authorization Handlers
            services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, BandStaffPermissionHandler>();
            services.AddScoped<IAuthorizationHandler, SelfAccessHandler>();
            services.AddScoped<IAuthorizationHandler, GuardianStudentAccessHandler>();
            services.AddScoped<IAuthorizationHandler, ScholarshipApprovalHandler>();
            services.AddScoped<IAuthorizationHandler, StudentResourceAuthorizationHandler>();

            // Register Custom Authorization Service
            services.AddScoped<IPermissionService, PermissionService>();

            // Configure Authorization Policies
            services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy("StudentOnly", policy => policy.RequireRole(Roles.Student));
                options.AddPolicy("GuardianOnly", policy => policy.RequireRole(Roles.Guardian));
                options.AddPolicy("RecruiterOnly", policy => policy.RequireRole(Roles.BandStaff));
                options.AddPolicy("DirectorOnly", policy => policy.RequireRole(Roles.Director));
                options.AddPolicy("BandStaffOnly", policy => policy.RequireRole(Roles.BandStaff, Roles.Director));

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
                    policy.RequireRole(Roles.BandStaff, Roles.Director);
                    policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.SendOffers));
                });

                options.AddPolicy("AdminAccess", policy =>
                {
                    policy.RequireRole(Roles.Director);
                    policy.Requirements.Add(new BandStaffPermissionRequirement(Permissions.ManageStaff));
                });
            });

            return services;
        }

        public static IServiceCollection AddPodiumCoreServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // SignalR — in-process for single-instance deployments (Railway pilot)
            services.AddSignalR();

            // Hangfire
            services.AddHangfire(config => config
                  .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));
            services.AddHangfireServer();

            // Background Jobs
            services.AddScoped<ExpireContactRequestsJob>();
            services.AddScoped<ExpireScholarshipOffersJob>();
            services.AddScoped<ArchiveInactiveStudentsJob>();
            services.AddScoped<CleanOldAuditLogsJob>();
            services.AddScoped<ProcessTranscodingQueueJob>();
            services.AddScoped<SendEmailNotificationsJob>();
            services.AddScoped<SearchAlertJob>();

            // Infrastructure Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IScholarshipService, ScholarshipService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVideoService, VideoService>();
            services.AddScoped<IDirectorService, DirectorService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IBandService, BandService>();
            services.AddScoped<IBandStaffService, BandStaffService>();
            services.AddScoped<ISavedSearchService, SavedSearchService>();

            // EMAIL SERVICE (Conditional Registration)
            if (environment.IsDevelopment())
            {
                services.AddTransient<IEmailService, MockEmailService>();
                Console.WriteLine("Using Mock Email Service (Check Console for output)");
            }
            else
            {
                services.AddTransient<IEmailService, EmailService>();
            }

            // CORS
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "https://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularDev", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With")
                          .AllowCredentials();
                });
            });

            // Storage Configuration — Cloudflare R2 (S3-compatible)
            var accountId = configuration["CloudflareR2:AccountId"];
            var accessKeyId = configuration["CloudflareR2:AccessKeyId"];
            var secretAccessKey = configuration["CloudflareR2:SecretAccessKey"];

            if (!string.IsNullOrWhiteSpace(accountId) && !string.IsNullOrWhiteSpace(accessKeyId))
            {
                services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                    new BasicAWSCredentials(accessKeyId, secretAccessKey),
                    new AmazonS3Config
                    {
                        ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                        ForcePathStyle = true
                    }));
                Console.WriteLine("Cloudflare R2 storage configured.");
            }
            else
            {
                // Fallback: standard AWS SDK config (reads from appsettings AWS section)
                var awsOptions = configuration.GetAWSOptions();
                services.AddDefaultAWSOptions(awsOptions);
                services.AddAWSService<IAmazonS3>();
                Console.WriteLine("Using AWS S3 for storage (no R2 config found).");
            }

            services.AddScoped<IVideoStorageService, AwsVideoStorageService>();
            services.AddScoped<IStorageService, AwsStorageService>();

            return services;
        }
    }
}