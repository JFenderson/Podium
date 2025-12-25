using Amazon.S3;
using Hangfire;
using Hangfire.SqlServer;
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

        public static IServiceCollection AddPodiumIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
                options.RequireHttpsMetadata = false;
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
            // SignalR
            services.AddSignalR();

            // Hangfire
            services.AddHangfire(config => config
                  .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                  {
                      CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                      SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                      QueuePollInterval = TimeSpan.Zero,
                      UseRecommendedIsolationLevel = true,
                      DisableGlobalLocks = true,
                      PrepareSchemaIfNecessary = true // <--- ADD THIS LINE to fix the crash
                  }));
            services.AddHangfireServer();

            // Background Jobs
            services.AddScoped<ExpireContactRequestsJob>();
            services.AddScoped<ExpireScholarshipOffersJob>();
            services.AddScoped<ArchiveInactiveStudentsJob>();
            services.AddScoped<CleanOldAuditLogsJob>();
            services.AddScoped<ProcessTranscodingQueueJob>();
            services.AddScoped<SendEmailNotificationsJob>();

            // Infrastructure Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Note: IAuthService registered twice in original, keeping both interfaces mapping to AuthService
            services.AddScoped<Podium.Application.Interfaces.IAuthService, AuthService>();
            services.AddScoped<IAuthService, Podium.Infrastructure.Services.AuthService>(); // Assuming this interface is available in Infra

            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IScholarshipService, ScholarshipService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVideoService, VideoService>();
            services.AddScoped<IDirectorService, DirectorService>();
            services.AddScoped<IGuardianService, GuardianService>();
            services.AddScoped<IBandService, BandService>();

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
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", policy =>
                {
                    policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" })
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp", policy =>
                {
                    policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" })
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Storage Configuration
            var storageProvider = configuration["StorageProvider"];

            if (storageProvider == "AWS")
            {
                var awsOptions = configuration.GetAWSOptions();
                services.AddDefaultAWSOptions(awsOptions);
                services.AddAWSService<IAmazonS3>();
                services.AddScoped<IVideoStorageService, AwsVideoStorageService>();
                Console.WriteLine("Using AWS S3 for video storage.");
            }
            else
            {
                services.AddScoped<IVideoStorageService, AzureVideoStorageService>();
                Console.WriteLine("Using Azure Blob Storage for video storage.");
            }

            var storageConnectionString = configuration["AzureStorage:ConnectionString"];
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                services.AddScoped<IStorageService, AzureBlobStorageService>();
                services.AddScoped<Podium.Core.Interfaces.IStorageService, AzureBlobStorageService>();
                Console.WriteLine("Azure Blob Storage configured successfully.");
            }
            else
            {
                Console.WriteLine("Azure Storage connection string not configured. Storage service disabled.");
            }

            return services;
        }
    }
}