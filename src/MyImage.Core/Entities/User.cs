using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Core User entity representing a registered user in the system.
/// This follows the MongoDB document structure defined in the database design specification.
/// Contains user authentication data, profile information, preferences, and statistics.
/// </summary>
public class User
{
    /// <summary>
    /// MongoDB ObjectId primary key - automatically generated
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Unique email address used for authentication and communication
    /// Must be unique across the system and properly validated
    /// </summary>
    [Required]
    [EmailAddress]
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password for secure authentication
    /// Never store plain text passwords - always hash before saving
    /// </summary>
    [Required]
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Email verification status - users must verify email before full access
    /// </summary>
    [BsonElement("emailVerified")]
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Token sent via email for email verification process
    /// Null when email is verified or no verification pending
    /// </summary>
    [BsonElement("emailVerificationToken")]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Token for password reset functionality
    /// Expires after a set time period for security
    /// </summary>
    [BsonElement("passwordResetToken")]
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Expiration date for password reset token
    /// After this date, token becomes invalid
    /// </summary>
    [BsonElement("passwordResetExpires")]
    public DateTime? PasswordResetExpires { get; set; }

    /// <summary>
    /// User profile information embedded document
    /// Contains personal details and contact information
    /// </summary>
    [BsonElement("profile")]
    public UserProfile Profile { get; set; } = new();

    /// <summary>
    /// Collection of user addresses for shipping and billing
    /// Supports multiple addresses with default designation
    /// </summary>
    [BsonElement("addresses")]
    public List<Address> Addresses { get; set; } = new();

    /// <summary>
    /// User preferences for notifications, defaults, and UI settings
    /// Customizes the user experience based on their choices
    /// </summary>
    [BsonElement("preferences")]
    public UserPreferences Preferences { get; set; } = new();

    /// <summary>
    /// User roles for authorization (customer, admin, employee)
    /// Controls access to different parts of the application
    /// </summary>
    [BsonElement("roles")]
    public List<string> Roles { get; set; } = new() { "customer" };

    /// <summary>
    /// User statistics and analytics data
    /// Tracks user behavior and business metrics
    /// </summary>
    [BsonElement("statistics")]
    public UserStatistics Statistics { get; set; } = new();

    /// <summary>
    /// System metadata for auditing and tracking
    /// Contains creation, update, and login information
    /// </summary>
    [BsonElement("metadata")]
    public UserMetadata Metadata { get; set; } = new();
}