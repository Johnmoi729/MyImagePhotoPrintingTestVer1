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
/// MongoDB database context that provides access to collections and manages database operations.
/// This class serves as the central access point for all MongoDB operations in our application.
/// It handles connection management, collection initialization, and provides a clean interface
/// for repository implementations while encapsulating MongoDB-specific configuration details.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<MongoDbContext> _logger;

    /// <summary>
    /// Initializes the MongoDB context with configuration and sets up database connection.
    /// This constructor configures the MongoDB client with appropriate settings for performance,
    /// reliability, and consistency based on the application configuration.
    /// It also sets up serialization conventions and creates necessary indexes for optimal query performance.
    /// </summary>
    /// <param name="settings">MongoDB configuration settings from appsettings.json</param>
    /// <param name="logger">Logger for tracking database operations and troubleshooting</param>
    public MongoDbContext(IOptions<MongoDbSettings> settings, ILogger<MongoDbContext> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configure MongoDB serialization conventions for .NET objects
        ConfigureSerializationConventions();

        // Create MongoDB client with performance and reliability settings
        var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
        ConfigureClientSettings(clientSettings);

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(_settings.DatabaseName);

        // Create indexes for optimal query performance if auto-creation is enabled
        if (_settings.AutoCreateIndexes)
        {
            _ = Task.Run(CreateIndexesAsync);
        }

        _logger.LogInformation("MongoDB context initialized for database: {DatabaseName}", _settings.DatabaseName);
    }

    /// <summary>
    /// Provides access to the Users collection with proper typing and configuration.
    /// This property returns a strongly-typed MongoDB collection that can be used by
    /// the UserRepository for all user-related database operations including queries, updates, and aggregations.
    /// </summary>
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    /// <summary>
    /// Provides access to the Photos collection with proper typing and configuration.
    /// This property returns a strongly-typed MongoDB collection for photo-related operations.
    /// The collection is configured with appropriate read and write concerns for photo data management.
    /// </summary>
    public IMongoCollection<Photo> Photos => _database.GetCollection<Photo>("photos");

    /// <summary>
    /// Provides access to the print sizes reference collection.
    /// This collection contains the available print sizes, pricing information, and specifications
    /// that are used throughout the application for order processing and price calculations.
    /// </summary>
    public IMongoCollection<BsonDocument> PrintSizes => _database.GetCollection<BsonDocument>("printSizes");

    /// <summary>
    /// Provides access to the shopping carts collection for e-commerce functionality.
    /// Shopping carts have TTL (Time To Live) indexes to automatically expire abandoned carts
    /// and manage storage efficiently while providing a good user experience.
    /// </summary>
    public IMongoCollection<BsonDocument> ShoppingCarts => _database.GetCollection<BsonDocument>("shoppingCarts");

    /// <summary>
    /// Provides access to the orders collection for order management and tracking.
    /// Orders are critical business data with strict consistency requirements and comprehensive indexing
    /// to support order lookup, user history, and administrative reporting functions.
    /// </summary>
    public IMongoCollection<BsonDocument> Orders => _database.GetCollection<BsonDocument>("orders");

    /// <summary>
    /// Provides access to audit logs for security and compliance tracking.
    /// Audit logs capture important system events, user actions, and data changes
    /// for security monitoring, troubleshooting, and regulatory compliance purposes.
    /// </summary>
    public IMongoCollection<BsonDocument> AuditLogs => _database.GetCollection<BsonDocument>("auditLogs");

    /// <summary>
    /// Provides access to system configuration settings stored in MongoDB.
    /// This collection stores application configuration that can be updated without deployment,
    /// feature flags, and other dynamic settings that control application behavior.
    /// </summary>
    public IMongoCollection<BsonDocument> SystemConfiguration => _database.GetCollection<BsonDocument>("systemConfiguration");

    /// <summary>
    /// Provides direct access to the underlying MongoDB database for advanced operations.
    /// This property allows access to database-level operations like transactions,
    /// administrative commands, and collection management for advanced use cases.
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Configures MongoDB serialization conventions for .NET object mapping.
    /// These conventions determine how .NET objects are serialized to BSON and stored in MongoDB.
    /// Proper configuration ensures consistent data representation and optimal query performance.
    /// </summary>
    private void ConfigureSerializationConventions()
    {
        // Configure camelCase naming convention for consistent JSON-like field names
        var camelCaseConvention = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };

        // Configure enum serialization to use string values instead of numeric
        // This makes the database more readable and resilient to enum value changes
        var enumConvention = new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String)
        };

        // Configure DateTime serialization to use UTC and ISO format
        // Ensures consistent date handling across different timezones and systems
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));

        // Apply conventions to all types in our domain
        ConventionRegistry.Register("CamelCase", camelCaseConvention, t => true);
        ConventionRegistry.Register("EnumStringConvention", enumConvention, t => true);

        // Configure specific serializers for value objects and complex types
        ConfigureCustomSerializers();

        _logger.LogDebug("MongoDB serialization conventions configured");
    }

    /// <summary>
    /// Configures custom serializers for complex types and value objects.
    /// Custom serializers ensure that complex business objects are properly
    /// serialized and deserialized while maintaining their invariants and behavior.
    /// </summary>
    private void ConfigureCustomSerializers()
    {
        // Configure ObjectId serialization for string properties
        // This allows our entities to use string IDs while MongoDB uses ObjectIds internally
        BsonSerializer.RegisterSerializer(typeof(string), new StringSerializer(BsonType.ObjectId));

        // Register serializers for value objects if we implement them
        // Example: BsonSerializer.RegisterSerializer(typeof(Money), new MoneySerializer());

        _logger.LogDebug("Custom MongoDB serializers registered");
    }

    /// <summary>
    /// Configures MongoDB client settings for optimal performance and reliability.
    /// These settings control connection pooling, timeouts, read preferences, and write concerns
    /// to balance performance, consistency, and reliability based on application requirements.
    /// </summary>
    /// <param name="clientSettings">MongoDB client settings to configure</param>
    private void ConfigureClientSettings(MongoClientSettings clientSettings)
    {
        // Configure connection pooling for optimal resource utilization
        clientSettings.MaxConnectionPoolSize = _settings.MaxConnectionPoolSize;
        clientSettings.MinConnectionPoolSize = 5;
        clientSettings.MaxConnectionIdleTime = TimeSpan.FromMinutes(10);
        clientSettings.MaxConnectionLifeTime = TimeSpan.FromMinutes(30);

        // Configure operation timeouts to prevent hanging operations
        clientSettings.SocketTimeout = TimeSpan.FromMilliseconds(_settings.OperationTimeoutMs);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);

        // Configure read preference for load balancing and consistency
        clientSettings.ReadPreference = _settings.ReadPreference switch
        {
            "Secondary" => ReadPreference.Secondary,
            "PrimaryPreferred" => ReadPreference.PrimaryPreferred,
            "SecondaryPreferred" => ReadPreference.SecondaryPreferred,
            _ => ReadPreference.Primary
        };

        // Configure write concern for durability vs performance balance
        clientSettings.WriteConcern = _settings.WriteConcern switch
        {
            "Majority" => WriteConcern.WMajority,
            "Unacknowledged" => WriteConcern.Unacknowledged,
            _ => WriteConcern.Acknowledged
        };

        // Configure read concern for consistency requirements
        clientSettings.ReadConcern = ReadConcern.Local;

        _logger.LogDebug("MongoDB client settings configured with pool size: {PoolSize}, timeout: {Timeout}ms",
            _settings.MaxConnectionPoolSize, _settings.OperationTimeoutMs);
    }

    /// <summary>
    /// Creates database indexes for optimal query performance.
    /// Indexes are created based on common query patterns identified in the application
    /// to ensure fast response times for user operations and administrative queries.
    /// This method runs asynchronously during startup to avoid blocking application initialization.
    /// </summary>
    private async Task CreateIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Creating MongoDB indexes for optimal query performance");

            // Create indexes for Users collection
            await CreateUserIndexesAsync();

            // Create indexes for Photos collection  
            await CreatePhotoIndexesAsync();

            // Create indexes for other collections
            await CreateOrderIndexesAsync();
            await CreateAuditLogIndexesAsync();

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
        }
    }

    /// <summary>
    /// Creates optimized indexes for the Users collection.
    /// These indexes support fast user lookup during authentication, user management,
    /// and administrative operations while ensuring unique constraints are enforced.
    /// </summary>
    private async Task CreateUserIndexesAsync()
    {
        var users = Users;

        // Unique index on email for fast login and duplicate prevention
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Background = true }));

        // Index on email verification token for email verification process
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.EmailVerificationToken),
            new CreateIndexOptions { Background = true, Sparse = true }));

        // Index on password reset token for password recovery
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.PasswordResetToken),
            new CreateIndexOptions { Background = true, Sparse = true }));

        // Index on roles for authorization queries
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Roles),
            new CreateIndexOptions { Background = true }));

        // Index on account status for user management
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending("metadata.accountStatus"),
            new CreateIndexOptions { Background = true }));

        // Index on creation date for user analytics
        await users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Descending("metadata.createdAt"),
            new CreateIndexOptions { Background = true }));

        _logger.LogDebug("User collection indexes created");
    }

    /// <summary>
    /// Creates optimized indexes for the Photos collection.
    /// These indexes support fast photo gallery loading, search functionality,
    /// and photo management operations while maintaining good performance with large photo collections.
    /// </summary>
    private async Task CreatePhotoIndexesAsync()
    {
        var photos = Photos;

        // Compound index on userId and upload date for user photo galleries
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys
                .Ascending(p => p.UserId)
                .Descending("fileInfo.uploadedAt"),
            new CreateIndexOptions { Background = true }));

        // Index on processing status for background job processing
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys.Ascending("processing.status"),
            new CreateIndexOptions { Background = true }));

        // Index on tags for search functionality (multikey index)
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys.Ascending(p => p.Tags),
            new CreateIndexOptions { Background = true }));

        // Index on deleted flag for filtering deleted photos
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys.Ascending("flags.isDeleted"),
            new CreateIndexOptions { Background = true }));

        // Compound index for user photos excluding deleted
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys
                .Ascending(p => p.UserId)
                .Ascending("flags.isDeleted")
                .Descending("fileInfo.uploadedAt"),
            new CreateIndexOptions { Background = true }));

        // Text index for search functionality across filename and notes
        await photos.Indexes.CreateOneAsync(new CreateIndexModel<Photo>(
            Builders<Photo>.IndexKeys.Text("fileInfo.originalFilename").Text(p => p.UserNotes),
            new CreateIndexOptions { Background = true }));

        _logger.LogDebug("Photo collection indexes created");
    }

    /// <summary>
    /// Creates indexes for order-related collections.
    /// These indexes support order lookup, user order history, and administrative reporting
    /// while ensuring fast performance for critical e-commerce operations.
    /// </summary>
    private async Task CreateOrderIndexesAsync()
    {
        var orders = Orders;

        // Index on order number for fast order lookup
        await orders.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("orderNumber"),
            new CreateIndexOptions { Unique = true, Background = true }));

        // Compound index on userId and order date for user order history
        await orders.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys
                .Ascending("userId")
                .Descending("metadata.createdAt"),
            new CreateIndexOptions { Background = true }));

        // Index on order status for administrative filtering
        await orders.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("status.current"),
            new CreateIndexOptions { Background = true }));

        // Index on creation date for reporting and analytics
        await orders.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Descending("metadata.createdAt"),
            new CreateIndexOptions { Background = true }));

        _logger.LogDebug("Order collection indexes created");
    }

    /// <summary>
    /// Creates indexes for audit log collection.
    /// These indexes support security monitoring, compliance reporting, and troubleshooting
    /// by enabling fast queries across different audit log dimensions.
    /// </summary>
    private async Task CreateAuditLogIndexesAsync()
    {
        var auditLogs = AuditLogs;

        // Index on timestamp for chronological queries
        await auditLogs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Descending("timestamp"),
            new CreateIndexOptions { Background = true }));

        // Index on event type for filtering by event categories
        await auditLogs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("event.type"),
            new CreateIndexOptions { Background = true }));

        // Index on actor userId for user activity tracking
        await auditLogs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("actor.userId"),
            new CreateIndexOptions { Background = true, Sparse = true }));

        // Compound index for target-specific audit queries
        await auditLogs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys
                .Ascending("target.type")
                .Ascending("target.id"),
            new CreateIndexOptions { Background = true }));

        _logger.LogDebug("Audit log collection indexes created");
    }
}