using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Infrastructure.Data.MongoDb;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// MongoDB implementation of the User repository interface.
/// This class provides concrete data access methods for user operations using MongoDB as the storage backend.
/// It implements all user-related database operations including authentication, profile management, and user analytics.
/// The implementation uses MongoDB's efficient query capabilities and follows established patterns for data access.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// Initializes the user repository with MongoDB context and logging.
    /// Sets up the MongoDB collection reference and configures logging for data access operations.
    /// The repository uses the configured MongoDB context to ensure consistent connection management.
    /// </summary>
    /// <param name="mongoContext">MongoDB database context providing access to collections</param>
    /// <param name="logger">Logger for tracking repository operations and troubleshooting</param>
    public UserRepository(MongoDbContext mongoContext, ILogger<UserRepository> logger)
    {
        _users = mongoContext.Users;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a user by their unique MongoDB ObjectId.
    /// This method is used throughout the application for user lookups during authentication,
    /// authorization, and user-specific operations. It returns the complete user document
    /// including profile information, preferences, and metadata.
    /// </summary>
    /// <param name="id">MongoDB ObjectId as string identifier</param>
    /// <returns>Complete user entity or null if not found</returns>
    public async Task<User?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Retrieving user by ID: {UserId}", id);

            // Use MongoDB's efficient primary key lookup
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("User found: {Email}", user.Email);
            }
            else
            {
                _logger.LogDebug("User not found for ID: {UserId}", id);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Finds a user by their email address for authentication and duplicate checking.
    /// This method uses the unique email index for fast lookups during login operations.
    /// Email comparison is case-insensitive to improve user experience and prevent issues
    /// with different email casing conventions.
    /// </summary>
    /// <param name="email">User's email address (case-insensitive)</param>
    /// <returns>User entity matching the email or null if not found</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            _logger.LogDebug("Retrieving user by email: {Email}", email);

            // Create case-insensitive email filter using regex
            var filter = Builders<User>.Filter.Regex(u => u.Email,
                new MongoDB.Bson.BsonRegularExpression($"^{email}$", "i"));

            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("User found for email: {Email}", email);
            }
            else
            {
                _logger.LogDebug("No user found for email: {Email}", email);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a user by their email verification token.
    /// This method supports the email verification workflow by finding users who have
    /// pending email verification. Tokens should be unique and time-limited for security.
    /// </summary>
    /// <param name="token">Email verification token from verification email</param>
    /// <returns>User entity with matching verification token or null if invalid</returns>
    public async Task<User?> GetByEmailVerificationTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Retrieving user by email verification token");

            var filter = Builders<User>.Filter.Eq(u => u.EmailVerificationToken, token);
            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("User found for email verification token");
            }
            else
            {
                _logger.LogDebug("No user found for email verification token");
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email verification token");
            throw;
        }
    }

    /// <summary>
    /// Finds a user by their password reset token for password recovery operations.
    /// This method supports secure password reset functionality by validating reset tokens.
    /// The implementation checks both token validity and expiration to ensure security.
    /// </summary>
    /// <param name="token">Password reset token from recovery email</param>
    /// <returns>User entity with valid reset token or null if token is invalid/expired</returns>
    public async Task<User?> GetByPasswordResetTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Retrieving user by password reset token");

            // Create filter that checks both token validity and expiration
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.PasswordResetToken, token),
                Builders<User>.Filter.Gt(u => u.PasswordResetExpires, DateTime.UtcNow)
            );

            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("User found for valid password reset token");
            }
            else
            {
                _logger.LogDebug("No user found for password reset token (invalid or expired)");
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by password reset token");
            throw;
        }
    }

    /// <summary>
    /// Creates a new user account in the database.
    /// This method handles user registration by inserting a new user document with all required fields.
    /// It automatically sets creation timestamps and initializes default values for new users.
    /// MongoDB will generate a unique ObjectId for the new user automatically.
    /// </summary>
    /// <param name="user">Complete user entity with all registration information</param>
    /// <returns>Created user entity with MongoDB-generated ID</returns>
    public async Task<User> CreateAsync(User user)
    {
        try
        {
            _logger.LogDebug("Creating new user: {Email}", user.Email);

            // Set creation and update timestamps
            user.Metadata.CreatedAt = DateTime.UtcNow;
            user.Metadata.UpdatedAt = DateTime.UtcNow;

            // Insert the user document and let MongoDB generate the ObjectId
            await _users.InsertOneAsync(user);

            _logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.Id, user.Email);

            return user;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("Attempted to create user with duplicate email: {Email}", user.Email);
            throw new InvalidOperationException("A user with this email address already exists.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing user's information in the database.
    /// This method performs a complete document replacement while preserving the original ID.
    /// It automatically updates the modification timestamp to track when changes were made.
    /// The update operation is atomic and will fail if the user doesn't exist.
    /// </summary>
    /// <param name="id">User's unique identifier</param>
    /// <param name="user">Updated user entity with new information</param>
    /// <returns>True if update succeeded, false if user not found</returns>
    public async Task<bool> UpdateAsync(string id, User user)
    {
        try
        {
            _logger.LogDebug("Updating user: {UserId}", id);

            // Ensure the ID remains consistent and update timestamp
            user.Id = id;
            user.Metadata.UpdatedAt = DateTime.UtcNow;

            // Replace the entire document with the updated version
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            var result = await _users.ReplaceOneAsync(filter, user);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("User updated successfully: {UserId}", id);
            }
            else
            {
                _logger.LogWarning("User update failed - user not found: {UserId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Performs a soft delete of a user account by updating the account status.
    /// This method preserves user data for business analytics and order history while
    /// making the account inaccessible for login and other operations.
    /// Soft deletion is preferred over hard deletion to maintain referential integrity.
    /// </summary>
    /// <param name="id">User's unique identifier</param>
    /// <returns>True if deletion succeeded, false if user not found</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            _logger.LogDebug("Soft deleting user: {UserId}", id);

            // Update account status to 'deleted' instead of removing the document
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            var update = Builders<User>.Update
                .Set("metadata.accountStatus", "deleted")
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _users.UpdateOneAsync(filter, update);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogInformation("User soft deleted successfully: {UserId}", id);
            }
            else
            {
                _logger.LogWarning("User soft deletion failed - user not found: {UserId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting user: {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Checks if an email address is already registered in the system.
    /// This method provides an efficient way to validate email uniqueness during registration
    /// without retrieving the complete user document. It's optimized for existence checking only.
    /// </summary>
    /// <param name="email">Email address to check for existence</param>
    /// <returns>True if email is already registered, false otherwise</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            _logger.LogDebug("Checking email existence: {Email}", email);

            // Use case-insensitive email comparison and count documents for efficiency
            var filter = Builders<User>.Filter.Regex(u => u.Email,
                new MongoDB.Bson.BsonRegularExpression($"^{email}$", "i"));

            var count = await _users.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
            var exists = count > 0;

            _logger.LogDebug("Email existence check result: {Email} exists: {Exists}", email, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Updates user login tracking information after successful authentication.
    /// This method efficiently updates only the login-related fields without affecting
    /// other user data. It's called after each successful login to maintain user analytics.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="loginTime">Timestamp of the login event</param>
    /// <returns>True if login tracking update succeeded</returns>
    public async Task<bool> UpdateLastLoginAsync(string userId, DateTime loginTime)
    {
        try
        {
            _logger.LogDebug("Updating last login for user: {UserId}", userId);

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set("metadata.lastLoginAt", loginTime)
                .Inc("metadata.loginCount", 1)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _users.UpdateOneAsync(filter, update);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("Login tracking updated for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Login tracking update failed - user not found: {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating login tracking for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves users with specific roles for administrative and authorization purposes.
    /// This method supports user management functionality by finding users with particular roles
    /// such as administrators, employees, or other special designations.
    /// </summary>
    /// <param name="role">Role name to search for (e.g., "admin", "employee")</param>
    /// <returns>Collection of users with the specified role</returns>
    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
    {
        try
        {
            _logger.LogDebug("Retrieving users by role: {Role}", role);

            // Use the multikey index on roles array for efficient querying
            var filter = Builders<User>.Filter.AnyEq(u => u.Roles, role);
            var users = await _users.Find(filter).ToListAsync();

            _logger.LogDebug("Found {Count} users with role: {Role}", users.Count, role);

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by role: {Role}", role);
            throw;
        }
    }

    /// <summary>
    /// Updates specific user statistics efficiently without loading the entire document.
    /// This method is optimized for frequently updated statistics like order counts,
    /// lifetime value, and last activity dates. It uses MongoDB's atomic update operations.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="statisticsUpdate">Dictionary of statistics fields to update</param>
    /// <returns>True if statistics update succeeded</returns>
    public async Task<bool> UpdateUserStatisticsAsync(string userId, Dictionary<string, object> statisticsUpdate)
    {
        try
        {
            _logger.LogDebug("Updating user statistics: {UserId}", userId);

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var updateBuilder = Builders<User>.Update.Set("metadata.updatedAt", DateTime.UtcNow);

            // Build update operations for each statistic
            foreach (var stat in statisticsUpdate)
            {
                var fieldPath = $"statistics.{stat.Key}";
                updateBuilder = updateBuilder.Set(fieldPath, stat.Value);
            }

            var result = await _users.UpdateOneAsync(filter, updateBuilder);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("User statistics updated: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("User statistics update failed - user not found: {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user statistics: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Clears sensitive authentication tokens for security purposes.
    /// This method removes password reset tokens and email verification tokens
    /// after they've been used or when they need to be invalidated for security reasons.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="clearPasswordResetToken">Whether to clear password reset token</param>
    /// <param name="clearEmailVerificationToken">Whether to clear email verification token</param>
    /// <returns>True if token clearing succeeded</returns>
    public async Task<bool> ClearAuthenticationTokensAsync(string userId, bool clearPasswordResetToken = false, bool clearEmailVerificationToken = false)
    {
        try
        {
            _logger.LogDebug("Clearing authentication tokens for user: {UserId}", userId);

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var updateBuilder = Builders<User>.Update.Set("metadata.updatedAt", DateTime.UtcNow);

            if (clearPasswordResetToken)
            {
                updateBuilder = updateBuilder
                    .Unset(u => u.PasswordResetToken)
                    .Unset(u => u.PasswordResetExpires);
            }

            if (clearEmailVerificationToken)
            {
                updateBuilder = updateBuilder.Unset(u => u.EmailVerificationToken);
            }

            var result = await _users.UpdateOneAsync(filter, updateBuilder);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("Authentication tokens cleared for user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Token clearing failed - user not found: {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authentication tokens for user: {UserId}", userId);
            throw;
        }
    }
}