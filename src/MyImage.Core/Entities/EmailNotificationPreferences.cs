using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Email notification preferences by category
/// Gives users granular control over communications
/// </summary>
public class EmailNotificationPreferences
{
    /// <summary>
    /// Order status updates (processing, shipped, delivered)
    /// Critical for order tracking - recommended to keep enabled
    /// </summary>
    [BsonElement("orderUpdates")]
    public bool OrderUpdates { get; set; } = true;

    /// <summary>
    /// Marketing promotions and special offers
    /// Optional - respects user's marketing preferences
    /// </summary>
    [BsonElement("promotions")]
    public bool Promotions { get; set; } = false;

    /// <summary>
    /// Reminders about photos uploaded but not ordered
    /// Helps convert uploads to sales
    /// </summary>
    [BsonElement("photoReminders")]
    public bool PhotoReminders { get; set; } = true;
}