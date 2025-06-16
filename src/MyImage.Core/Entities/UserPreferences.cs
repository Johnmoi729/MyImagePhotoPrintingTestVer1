using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// User preferences for customizing the application experience
/// Controls notifications, defaults, and UI behavior
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Email notification preferences by category
    /// Allows users to control what emails they receive
    /// </summary>
    [BsonElement("emailNotifications")]
    public EmailNotificationPreferences EmailNotifications { get; set; } = new();

    /// <summary>
    /// Default print settings to pre-populate forms
    /// Saves time for repeat customers with consistent preferences
    /// </summary>
    [BsonElement("defaultPrintSettings")]
    public DefaultPrintSettings DefaultPrintSettings { get; set; } = new();

    /// <summary>
    /// User's preferred language for localization
    /// </summary>
    [BsonElement("language")]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Preferred currency for pricing display
    /// </summary>
    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// User's timezone for proper date/time display
    /// </summary>
    [BsonElement("timezone")]
    public string Timezone { get; set; } = "America/New_York";
}