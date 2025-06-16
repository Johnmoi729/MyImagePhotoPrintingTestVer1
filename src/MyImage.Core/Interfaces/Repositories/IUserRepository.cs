using MyImage.Core.Entities;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// Defines the contract for user data access without exposing implementation details.
/// This interface allows the domain layer to work with users without knowing about MongoDB specifics.
/// The infrastructure layer will implement these methods using MongoDB.Driver.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// Used throughout the application for user lookups during authentication and operations.
    /// </summary>
    /// <param name=id">MongoDB ObjectId as string</param>
    /// <returns>User entity or null if not found</returns>
    Task<User?> GetByIdAsync(string id);

    /// <summary>
    /// Finds a user by email address for authentication purposes.
    /// Email is unique in our system, so this should return at most one user.
    /// Critical for login functionality and duplicate email prevention.
    /// </summary>
    /// <param name="email">User's email address (case-insensitive)</param>
    /// <returns>User entity or null if not found</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their email verification token.
    /// Used during the email verification process when users click verification links.
    /// Token should be unique and expire after a reasonable time period.
    /// </summary>
    /// <param name="token">Email verification token from the verification email</param>
    /// <returns>User entity or null if token is invalid/expired</returns>
    Task<User?> GetByEmailVerificationTokenAsync(string token);

    /// <summary>
    /// Finds a user by their password reset token.
    /// Used when users follow password reset links from their email.
    /// Tokens should expire after a short time (usually 1-24 hours) for security.
    /// </summary>
    /// <param name="token">Password reset token from the reset email</param>
    /// <returns>User entity or null if token is invalid/expired</returns>
    Task<User?> GetByPasswordResetTokenAsync(string token);

    /// <summary>
    /// Creates a new user account in the database.
    /// Called during user registration after validation and password hashing.
    /// Should handle duplicate email detection and return the created user with ID.
    /// </summary>
    /// <param name="user">User entity with all required fields populated</param>
    /// <returns>Created user entity with MongoDB-generated ID</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Updates an existing user's information.
    /// Used for profile updates, preference changes, and system updates (login tracking).
    /// Should update the UpdatedAt timestamp automatically.
    /// </summary>
    /// <param name="id">User's unique identifier</param>
    /// <param name="user">Updated user entity with new values</param>
    /// <returns>True if update succeeded, false if user not found</returns>
    Task<bool> UpdateAsync(string id, User user);

    /// <summary>
    /// Performs a soft delete of a user account.
    /// Sets account status to 'deleted' rather than removing the record entirely.
    /// Preserves data for business analytics while making account inaccessible.
    /// </summary>
    /// <param name="id">User's unique identifier</param>
    /// <returns>True if deletion succeeded, false if user not found</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Checks if an email address is already registered in the system.
    /// Used during registration to prevent duplicate accounts.
    /// More efficient than GetByEmailAsync when we only need existence check.
    /// </summary>
    /// <param name="email">Email address to check (case-insensitive)</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Updates user's login tracking information.
    /// Called after successful authentication to record login time and increment counter.
    /// Helps with user analytics and inactive account identification.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="loginTime">Timestamp of the login event</param>
    /// <returns>True if update succeeded</returns>
    Task<bool> UpdateLastLoginAsync(string userId, DateTime loginTime);

    /// <summary>
    /// Retrieves users with specific roles for admin functionality.
    /// Used to find administrators, employees, or other special role users.
    /// Supports authorization and user management features.
    /// </summary>
    /// <param name="role">Role name to search for (e.g., "admin", "employee")</param>
    /// <returns>Collection of users with the specified role</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
}