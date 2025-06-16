using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using MyImage.Infrastructure.Data.MongoDb;
using MyImage.Infrastructure.Data.Repositories;
using MyImage.Infrastructure.Services;
using MyImage.Application.Interfaces;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Application.Mappings;

namespace MyImage.API;

/// <summary>
/// Application entry point and service configuration.
/// This class sets up the complete dependency injection container, middleware pipeline,
/// and application configuration for the MyImage photo printing service.
/// It configures authentication, logging, rate limiting, CORS, and all business services
/// while maintaining proper separation of concerns and clean architecture principles.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog for structured logging
        ConfigureSerilog(builder);

        // Add services to the container
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        ConfigurePipeline(app);

        app.Run();
    }

    /// <summary>
    /// Configures Serilog for structured logging throughout the application.
    /// Sets up console and file logging with appropriate log levels and formatting
    /// for both development and production environments.
    /// </summary>
    /// <param name="builder">Web application builder for configuration</param>
    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MyImage.API")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .CreateLogger();

        builder.Host.UseSerilog();

        Log.Information("MyImage API starting up in {Environment} environment", builder.Environment.EnvironmentName);
    }

    /// <summary>
    /// Configures all application services in the dependency injection container.
    /// This method sets up all layers of the application including data access,
    /// business logic, external services, and cross-cutting concerns like authentication and caching.
    /// </summary>
    /// <param name="services">Service collection for dependency registration</param>
    /// <param name="configuration">Application configuration for service setup</param>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add controllers with API conventions
        services.AddControllers(options =>
        {
            // Add global filters and conventions
            options.SuppressAsyncSuffixInActionNames = false;
        });

        // Configure API documentation
        ConfigureSwagger(services);

        // Configure authentication and authorization
        ConfigureAuthentication(services, configuration);

        // Configure CORS for frontend access
        ConfigureCors(services, configuration);

        // Configure rate limiting for API protection
        ConfigureRateLimiting(services, configuration);

        // Configure data access layer
        ConfigureDataAccess(services, configuration);

        // Configure business services
        ConfigureBusinessServices(services);

        // Configure infrastructure services
        ConfigureInfrastructureServices(services, configuration);

        // Configure AutoMapper for object mapping
        ConfigureAutoMapper(services);

        // Configure caching
        ConfigureCaching(services, configuration);

        // Configure health checks
        ConfigureHealthChecks(services, configuration);

        Log.Information("All services configured successfully");
    }

    /// <summary>
    /// Configures Swagger/OpenAPI documentation for the API.
    /// Sets up comprehensive API documentation with authentication support
    /// and proper schema generation for all endpoints.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MyImage Photo Printing API",
                Version = "v1",
                Description = "REST API for MyImage photo printing service",
                Contact = new OpenApiContact
                {
                    Name = "MyImage Support",
                    Email = "support@myimage.com"
                }
            });

            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
    }

    /// <summary>
    /// Configures JWT authentication and authorization services.
    /// Sets up secure token validation with proper key management and claim extraction
    /// for protecting API endpoints and user session management.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing JWT settings</param>
    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = true; // Require HTTPS in production
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Remove default 5-minute clock skew
                RequireExpirationTime = true
            };

            // Configure JWT events for logging and custom handling
            x.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT token validated for user: {UserId}",
                        context.Principal?.FindFirst("sub")?.Value);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Define authorization policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("CustomerOrAdmin", policy => policy.RequireRole("customer", "admin"));
        });
    }

    /// <summary>
    /// Configures CORS (Cross-Origin Resource Sharing) for frontend applications.
    /// Sets up secure cross-origin access policies while maintaining security boundaries
    /// and supporting different frontend deployment scenarios.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing CORS settings</param>
    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CorsSettings");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });

            // Separate policy for development
            options.AddPolicy("DevelopmentCorsPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    /// <summary>
    /// Configures rate limiting to protect against abuse and ensure fair usage.
    /// Sets up different rate limiting policies for various endpoint types
    /// with appropriate limits for file uploads, authentication, and general API access.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing rate limiting settings</param>
    private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            // General API requests
            options.AddFixedWindowLimiter("GeneralRequests", configure =>
            {
                configure.PermitLimit = rateLimitSettings.GetValue<int>("GeneralRequests:PermitLimit", 100);
                configure.Window = TimeSpan.FromMinutes(rateLimitSettings.GetValue<int>("GeneralRequests:WindowMinutes", 1));
                configure.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                configure.QueueLimit = 10;
            });

            // File upload requests (more restrictive)
            options.AddFixedWindowLimiter("FileUpload", configure =>
            {
                configure.PermitLimit = rateLimitSettings.GetValue<int>("FileUpload:PermitLimit", 20);
                configure.Window = TimeSpan.FromMinutes(rateLimitSettings.GetValue<int>("FileUpload:WindowMinutes", 1));
                configure.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                configure.QueueLimit = 5;
            });

            // Authentication requests (most restrictive)
            options.AddFixedWindowLimiter("Authentication", configure =>
            {
                configure.PermitLimit = rateLimitSettings.GetValue<int>("Authentication:PermitLimit", 10);
                configure.Window = TimeSpan.FromMinutes(rateLimitSettings.GetValue<int>("Authentication:WindowMinutes", 1));
                configure.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                configure.QueueLimit = 3;
            });

            // Global fallback
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            };
        });
    }

    /// <summary>
    /// Configures data access layer services including MongoDB and repositories.
    /// Sets up database connections, repository implementations, and data access patterns
    /// while ensuring proper connection management and performance optimization.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing database settings</param>
    private static void ConfigureDataAccess(IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB
        services.AddMongoDb(configuration);

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();

        Log.Information("Data access layer configured");
    }

    /// <summary>
    /// Configures business logic services that implement application use cases.
    /// Registers all service interfaces with their implementations while maintaining
    /// proper dependency injection lifetime management for optimal performance and reliability.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    private static void ConfigureBusinessServices(IServiceCollection services)
    {
        // Register application services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPhotoService, PhotoService>();

        Log.Information("Business services configured");
    }

    /// <summary>
    /// Configures infrastructure services for external dependencies and cross-cutting concerns.
    /// Sets up email services, storage providers, image processing, and other infrastructure
    /// components that support the core business functionality.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration for infrastructure service setup</param>
    private static void ConfigureInfrastructureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register infrastructure services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IStorageService, GridFSStorageService>();
        services.AddScoped<IImageProcessingService, ImageProcessingService>();

        // Configure external service clients
        var emailSettings = configuration.GetSection("EmailSettings");
        if (emailSettings["Provider"] == "SendGrid")
        {
            // Configure SendGrid if using it
            services.Configure<SendGridEmailOptions>(emailSettings.GetSection("SendGrid"));
        }

        Log.Information("Infrastructure services configured");
    }

    /// <summary>
    /// Configures AutoMapper for automatic object-to-object mapping.
    /// Sets up mapping profiles for converting between domain entities and DTOs
    /// while maintaining clean separation between layers and reducing boilerplate code.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    private static void ConfigureAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile));
        Log.Information("AutoMapper configured");
    }

    /// <summary>
    /// Configures caching services for improved performance.
    /// Sets up memory caching and distributed caching options based on deployment environment
    /// and performance requirements for frequently accessed data.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing cache settings</param>
    private static void ConfigureCaching(IServiceCollection services, IConfiguration configuration)
    {
        // Configure memory cache
        services.AddMemoryCache(options =>
        {
            var cacheSettings = configuration.GetSection("CacheSettings");
            options.SizeLimit = cacheSettings.GetValue<int>("MaxSizeLimit", 1000);
            options.CompactionPercentage = cacheSettings.GetValue<double>("CompactionPercentage", 0.25);
        });

        // Configure distributed cache (Redis in production, in-memory for development)
        if (configuration.GetConnectionString("Redis") != null)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "MyImage";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        Log.Information("Caching services configured");
    }

    /// <summary>
    /// Configures health checks for monitoring application and dependency health.
    /// Sets up comprehensive health monitoring for database connections, external services,
    /// and application components to ensure system reliability and observability.
    /// </summary>
    /// <param name="services">Service collection for registration</param>
    /// <param name="configuration">Configuration containing health check settings</param>
    private static void ConfigureHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
            .AddMongoDb(
                configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017",
                name: "mongodb",
                tags: new[] { "db", "nosql" });

        Log.Information("Health checks configured");
    }

    /// <summary>
    /// Configures the HTTP request pipeline with middleware for request processing.
    /// Sets up the complete middleware pipeline including security, logging, error handling,
    /// and routing while maintaining proper order and configuration for each environment.
    /// </summary>
    /// <param name="app">Web application for pipeline configuration</param>
    private static void ConfigurePipeline(WebApplication app)
    {
        // Configure pipeline based on environment
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyImage API v1");
                c.RoutePrefix = string.Empty; // Serve Swagger at root
            });

            app.UseCors("DevelopmentCorsPolicy");
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts(); // Enable HTTP Strict Transport Security
            app.UseCors("DefaultCorsPolicy");
        }

        // Security headers middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Request logging middleware
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? Serilog.Events.LogEventLevel.Error
                : Serilog.Events.LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        // Core middleware pipeline
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        // Global error handling middleware
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // Map endpoints
        app.MapControllers();

        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        Log.Information("HTTP request pipeline configured");
    }
}
