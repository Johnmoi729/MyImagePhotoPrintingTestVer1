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
/// MongoDB connection and database configuration settings.
/// This class encapsulates all MongoDB-related configuration including connection strings,
/// database names, collection settings, and performance tuning options.
/// Configuration values are loaded from appsettings.json and validated during startup.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// MongoDB connection string including authentication and cluster information.
    /// Format: mongodb://username:password@host:port/database or mongodb+srv://cluster-url
    /// Should include connection pooling and timeout settings for production use.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Name of the database to use for this application.
    /// Separates our application data from other databases on the same MongoDB instance.
    /// Environment-specific databases (dev, staging, prod) should use different names.
    /// </summary>
    public string DatabaseName { get; set; } = "myimage";

    /// <summary>
    /// Maximum number of connections in the connection pool.
    /// Balances between resource usage and performance under load.
    /// Should be tuned based on application traffic patterns and MongoDB server capacity.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Timeout for database operations in milliseconds.
    /// Prevents long-running queries from blocking the application.
    /// Should be set based on expected query complexity and network latency.
    /// </summary>
    public int OperationTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Whether to enable automatic index creation for better query performance.
    /// Indexes are created based on common query patterns defined in repository implementations.
    /// Should be enabled in development but managed manually in production for better control.
    /// </summary>
    public bool AutoCreateIndexes { get; set; } = true;

    /// <summary>
    /// Read preference for database operations (Primary, Secondary, PrimaryPreferred, etc.).
    /// Affects read consistency and load distribution in replica set deployments.
    /// Primary ensures strong consistency, while secondary options can improve read performance.
    /// </summary>
    public string ReadPreference { get; set; } = "Primary";

    /// <summary>
    /// Write concern level for database operations (Acknowledged, Majority, etc.).
    /// Controls how writes are acknowledged and replicated across the cluster.
    /// Higher levels provide better durability but may impact write performance.
    /// </summary>
    public string WriteConcern { get; set; } = "Acknowledged";
}