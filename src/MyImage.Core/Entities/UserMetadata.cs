using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// System metadata for auditing and user tracking
/// Important for security, support, and analytics
/// </summary>
public class UserMetadata
{
    /// <summary>
    /// When the user account was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user record was last modified
    /// Updated on any profile or preference changes
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last logged in
    /// Used for inactive user identification
    /// </summary>
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Total number of login sessions
    /// Engagement metric
    /// </summary>
    [BsonElement("loginCount")]
    public int LoginCount { get; set; } = 0;

    /// <summary>
    /// How the user found us (organic, google, facebook, referral)
    /// Important for marketing attribution
    /// </summary>
    [BsonElement("registrationSource")]
    public string RegistrationSource { get; set; } = "organic";

    /// <summary>
    /// Referral code if user was referred by someone
    /// Used for referral program tracking
    /// </summary>
    [BsonElement("referralCode")]
    public string? ReferralCode { get; set; }

    /// <summary>
    /// Current account status (active, suspended, deleted)
    /// Controls user access to the system
    /// </summary>
    [BsonElement("accountStatus")]
    public string AccountStatus { get; set; } = "active";
}