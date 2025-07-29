using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.BackgroundServices;
using InnoHub.Core.Data;
using InnoHub.Core.IRepository;
using InnoHub.Core.Models;
using InnoHub.Middleware;
using InnoHub.MLService;
using InnoHub.ModelDTO;
using InnoHub.ModelDTO.ML;
using InnoHub.Repository.Repository;
using InnoHub.Service.EmailSenderService;
using InnoHub.Service.FileService;
using InnoHub.UnitOfWork;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace InnoHub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Enhanced Serilog Configuration with ML-specific logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                // ✅ NEW: ML Services logging
                .MinimumLevel.Override("InnoHub.Service.ML", LogEventLevel.Debug)
                .MinimumLevel.Override("InnoHub.Middleware.FlaskHealthCheck", LogEventLevel.Information)
                .MinimumLevel.Override("InnoHub.BackgroundServices", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "InnoHub")
                .Enrich.WithProperty("Version", "1.3.0-Flask") // ✅ Updated version
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine("logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine("logs", "ml-log-.txt"), // ✅ Separate ML logs
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    fileSizeLimitBytes: 5 * 1024 * 1024,
                    retainedFileCountLimit: 14,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                builder.Host.UseSerilog();

                CreateRequiredDirectories(builder);

                ConfigureServices(builder);

                var app = builder.Build();

                ConfigureMiddleware(app);

                await SeedData(app);

                // ✅ NEW: Check Flask ML services health at startup
                await CheckFlaskMLServicesHealth(app);

                Log.Information("✅ InnoHub started successfully with Flask ML integration");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "❌ Application startup failed due to exception");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void CreateRequiredDirectories(WebApplicationBuilder builder)
        {
            var directories = new[]
            {
                "logs",
                "wwwroot/Contracts",
                "wwwroot/Contracts/Archive",
                "wwwroot/images/products",
                "wwwroot/images/categories",
                "wwwroot/ProfileImages",
                "wwwroot/IdentityImages"
            };

            foreach (var dir in directories)
            {
                string fullPath = Path.Combine(builder.Environment.ContentRootPath, dir);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Log.Information("📁 Created directory: {DirectoryPath}", dir);
                }
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Database Configuration
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ✅ NEW: Flask ML Configuration
            builder.Services.Configure<FlaskAIConfiguration>(
                builder.Configuration.GetSection("FlaskAI"));
            builder.Services.Configure<MLFeaturesConfiguration>(
                builder.Configuration.GetSection("MLFeatures"));

            // ✅ NEW: Flask HTTP Client with Polly resilience
            builder.Services.AddHttpClient("FlaskAI", client =>
            {
                var config = builder.Configuration.GetSection("FlaskAI").Get<FlaskAIConfiguration>();
                client.BaseAddress = new Uri(config?.BaseUrl ?? "https://web-production-1112.up.railway.app");
                client.Timeout = TimeSpan.FromSeconds(config?.Timeout ?? 30);
                client.DefaultRequestHeaders.Add("User-Agent", "InnoHub-FlaskOnly/1.3.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Require-Service", "true");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // ✅ NEW: Flask Health Monitoring Background Service
            builder.Services.AddHostedService<FlaskHealthMonitorService>();

            // Add Health Checks service with Flask ML health checks
            builder.Services.AddHealthChecks()
                .AddCheck("Database", () => HealthCheckResult.Healthy("Database is healthy"))
                .AddCheck<MLHealthCheck>("Flask-ML-Services");

            // Identity Configuration
            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Authentication Configuration
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])),
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning("JWT Authentication failed: {Exception}", context.Exception?.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Log.Debug("JWT Token validated for user: {UserId}",
                            context.Principal?.FindFirst("userId")?.Value ?? "Unknown");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/signin-google";
            });

            // Rate Limiting Configuration
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = builder.Configuration.GetValue<int>("SecuritySettings:RateLimiting:RequestsPerMinute", 100),
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("Auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                // ✅ NEW: Rate limiting for ML endpoints
                options.AddPolicy("ML", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 50,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.RejectionStatusCode = 429;
            });

            // HTTP Logging
            builder.Services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPropertiesAndHeaders |
                                       Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponsePropertiesAndHeaders;
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
            });

            // ✅ NEW: Register ML Services
            RegisterMLServices(builder.Services);

            // Core Application Services Registration
            RegisterCoreServices(builder.Services);

            // CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins(
                                "http://localhost:5173",
                                "https://innov-hub-dashboard.vercel.app",
                                "https://innova-hub.premiumasp.net",
                                "https://innova-web-client-o9f2bm0yn-justmahmuds-projects.vercel.app"
                            )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                    });
            });

            builder.Services.AddHttpClient();

            // Controllers Configuration
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Enhanced API Documentation with ML endpoints
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "InnoHub API",
                    Version = "v1.3-Flask",
                    Description = "InnoHub API with Flask ML Integration - Recommendations, Spam Detection & Sales Prediction",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "InnoHub Development Team",
                        Email = "dev@innova-hub.com"
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });

                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }

        // ✅ NEW: ML Services Registration
        private static void RegisterMLServices(IServiceCollection services)
        {
            Log.Information("🤖 Registering Flask ML Services...");

            // ML Data Mapping Service
            services.AddScoped<IMLDataMappingService, MLDataMappingService>();

            // ML Recommendation Service
            services.AddScoped<IMLRecommendationService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("FlaskAI");
                var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FlaskAIConfiguration>>();
                var mlConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MLFeaturesConfiguration>>();
                var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
                var logger = provider.GetRequiredService<ILogger<MLRecommendationService>>();
                return new MLRecommendationService(httpClient, config, mlConfig, unitOfWork, logger);
            });

            // ML Spam Detection Service
            services.AddScoped<IMLSpamDetectionService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("FlaskAI");
                var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FlaskAIConfiguration>>();
                var mlConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MLFeaturesConfiguration>>();
                var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
                var mappingService = provider.GetRequiredService<IMLDataMappingService>();
                var logger = provider.GetRequiredService<ILogger<MLSpamDetectionService>>();
                return new MLSpamDetectionService(httpClient, config, mlConfig, unitOfWork, mappingService, logger);
            });

            // ML Sales Prediction Service
            services.AddScoped<IMLSalesPredictionService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("FlaskAI");
                var config = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FlaskAIConfiguration>>();
                var mlConfig = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MLFeaturesConfiguration>>();
                var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
                var mappingService = provider.GetRequiredService<IMLDataMappingService>();
                var logger = provider.GetRequiredService<ILogger<MLSalesPredictionService>>();
                return new MLSalesPredictionService(httpClient, config, mlConfig, unitOfWork, mappingService, logger);
            });

            // Health Check for ML Services
            services.AddScoped<MLHealthCheck>();

            Log.Information("✅ Flask ML Services registered successfully");
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            Log.Information("📦 Registering Core Services...");

            // Core Repository Services
            services.AddScoped<IAppUser, AppUserRepository>();
            services.AddScoped<IReport, ReportRepository>();
            services.AddScoped<IPaymentRefundLog, PaymentRefundLogRepository>();
            services.AddScoped<IPaymentFailureLog, PaymentFailureLogRepository>();
            services.AddScoped<IOrderReturnRequest, OrderReturnRequestRepository>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IDeliveryMethod, DeliveryMethodRepository>();
            services.AddScoped<IShippingAddress, ShippingAddressRepository>();
            services.AddScoped<IOrder, OrderRepository>();
            services.AddScoped<IWishlistItem, WishlistItemRepository>();
            services.AddScoped<IProductComment, ProductCommentRepository>();
            services.AddScoped<IProductRating, ProductRatingRepository>();
            services.AddScoped<IDeal, DealRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IAuth, Auth>();
            services.AddScoped<IProduct, ProductRepository>();
            services.AddScoped<ICategory, CategoryRepository>();
            services.AddScoped<ICart, CartRepository>();
            services.AddScoped<IWishlist, WishlistRepository>();

            // Deal-related services
            services.AddScoped<IDealMessage, DealMessageRepository>();
            services.AddScoped<IDealProfit, DealProfitRepository>();
            services.AddScoped<IDealTransaction, DealTransactionRepository>();
            services.AddScoped<IOrderItem, OrderItemRepository>();
            services.AddScoped<IDealChangeRequest, DealChangeRequestRepository>();
            services.AddScoped<IDealDeleteRequest, DealDeleteRequestRepository>();

            // Notification service
            services.AddScoped<INotification, NotificationRepository>();

            // Unit of Work pattern
            services.AddScoped<IUnitOfWork, UnitofWork>();

            // Other core services
            services.AddSingleton<InnoHub.Service.OtpService>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            Log.Information("✅ Core Services registered successfully");
        }

        // ✅ NEW: Polly Retry Policy for Flask API
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Log.Warning("Flask API retry attempt {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                    });
        }

        // ✅ NEW: Circuit Breaker Policy for Flask API
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(60),
                    onBreak: (exception, duration) =>
                    {
                        Log.Error("CRITICAL: Flask API circuit breaker opened for {Duration}s", duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        Log.Information("✅ Flask API circuit breaker reset");
                    });
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                        diagnosticContext.Set("UserId", httpContext.User.FindFirst("userId")?.Value);
                    }
                };
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InnoHub API v1.3-Flask");
                    c.RoutePrefix = "swagger";
                    c.DisplayRequestDuration();
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.ShowExtensions();
                });
            }

            // Health Check Endpoints
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Status = report.Status.ToString(),
                        Duration = report.TotalDuration.TotalMilliseconds,
                        Checks = report.Entries.Select(e => new
                        {
                            Name = e.Key,
                            Status = e.Value.Status.ToString(),
                            Duration = e.Value.Duration.TotalMilliseconds,
                            Description = e.Value.Description,
                            Data = e.Value.Data
                        })
                    }));
                }
            });

            // Global exception handling
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCors("AllowFrontend");

            // ✅ NEW: Flask Health Check Middleware
            app.UseMiddleware<FlaskHealthCheckMiddleware>();

            app.UseRateLimiter();

            app.UseHttpLogging();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ✅ Enhanced API Information Endpoint with Flask details
            app.MapGet("/api/info", () => Results.Ok(new
            {
                Name = "InnoHub API",
                Version = "1.3.0-Flask",
                Environment = app.Environment.EnvironmentName,
                Architecture = "Flask-Dependent API",
                MachineName = Environment.MachineName,
                StartTime = DateTime.UtcNow,
                MLDependency = new
                {
                    Required = true,
                    Provider = "Flask AI/ML API",
                    URL = "https://web-production-1112.up.railway.app",
                    Status = "Required for all ML operations",
                    NoLocalFallbacks = true
                },
                Features = new[]
                {
                    "JWT Authentication",
                    "Google OAuth",
                    "Rate Limiting",
                    "Health Checks",
                    "File Upload",
                    "Email Service",
                    "Payment Processing (Stripe)",
                    "Flask ML Recommendations (Required)",
                    "Flask ML Spam Detection (Required)",
                    "Flask ML Sales Prediction (Required)",
                    "Background Flask Health Monitoring"
                },
                MLEndpoints = new[]
                {
                    "/api/ml/recommendations - Flask Only",
                    "/api/ml/spam-detection - Flask Only",
                    "/api/ml/sales-prediction - Flask Only",
                    "/api/ml/health - Flask Status"
                }
            }))
            .WithName("GetAPIInfo")
            .WithTags("API", "Flask-Dependent");
        }

        private static async Task SeedData(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                Log.Information("🌱 Starting data seeding...");

                RoleDataSeeding.SeedData(userManager, roleManager);
                await IdentityUserDataSeeding.SeedUserAsync(userManager, roleManager);
                await CategoryDataSeeding.SeedPopularCategories(context);
                await DeliveryMethodDataSeeding.SeedDeliveryMethodsAsync(context);

                await context.SaveChangesAsync();
                Log.Information("✅ Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Error occurred during data seeding");
                throw;
            }
        }

        // ✅ NEW: Check Flask ML Services Health at Startup
        private static async Task CheckFlaskMLServicesHealth(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var recommendationService = scope.ServiceProvider.GetRequiredService<IMLRecommendationService>();

                Log.Information("🔍 Checking Flask ML services health...");

                var isHealthy = await recommendationService.IsServiceAvailableAsync();
                if (isHealthy)
                {
                    Log.Information("✅ Flask ML API is healthy and ready");
                }
                else
                {
                    Log.Warning("⚠️ Flask ML API is not responding - ML features will be unavailable");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "⚠️ Could not check Flask ML services health at startup");
            }
        }
    }

    // ✅ NEW: ML Health Check Implementation
    public class MLHealthCheck : IHealthCheck
    {
        private readonly IMLRecommendationService _recommendationService;

        public MLHealthCheck(IMLRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _recommendationService.IsServiceAvailableAsync();

                if (isHealthy)
                {
                    return HealthCheckResult.Healthy("Flask ML API is responding");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Flask ML API is not responding");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error checking Flask ML API", ex);
            }
        }
    }
}