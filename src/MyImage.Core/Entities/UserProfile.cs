using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// User profile information embedded within User document
/// Contains personal details and contact information
/// </summary>
public class UserProfile
{
    /// <summary>
    /// User's first name for personalization and communication
    /// </summary>
    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for formal communications and records
    /// </summary>
    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI - can be different from legal name
    /// Falls back to first name if not provided
    /// </summary>
    [BsonElement("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Date of birth for age verification and birthday promotions
    /// Optional field - some users may not provide
    /// </summary>
    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gender for demographics and personalization
    /// M, F, or null for prefer not to say
    /// </summary>
    [BsonElement("gender")]
    public string? Gender { get; set; }

    /// <summary>
    /// Phone number for order notifications and support
    /// Should include country code for international users
    /// </summary>
    [BsonElement("phoneNumber")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone verification status for security and SMS notifications
    /// </summary>
    [BsonElement("phoneVerified")]
    public bool PhoneVerified { get; set; } = false;

    /// <summary>
    /// URL to user's profile picture/avatar
    /// Stored in our file storage system
    /// </summary>
    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }
}