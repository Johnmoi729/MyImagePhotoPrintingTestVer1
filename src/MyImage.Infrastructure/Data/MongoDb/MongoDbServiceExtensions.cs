using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MyImage.Core.Entities;

namespace MyImage.Infrastructure.Data.MongoDb;

/// <summary>
/// Extension methods for dependency injection configuration of MongoDB services.
/// These methods provide a clean way to register MongoDB services in the application's
/// dependency injection container with proper configuration and lifetime management.
/// </summary>
public static class MongoDbServiceExtensions
{
    /// <summary>
    /// Registers MongoDB services in the dependency injection container.
    /// This method configures all MongoDB-related services including the context,
    /// settings, and any custom services needed for database operations.
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="configuration">Application configuration containing MongoDB settings</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MongoDB settings from configuration
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

        // Register MongoDB context as singleton for efficient connection pooling
        services.AddSingleton<MongoDbContext>();

        // Register health checks for MongoDB connectivity monitoring
        services.AddHealthChecks()
            .AddMongoDb(configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017",
                name: "mongodb",
                tags: new[] { "db", "nosql", "mongodb" });

        return services;
    }
}