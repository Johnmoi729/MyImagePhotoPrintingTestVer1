using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// User statistics for business analytics and personalization
/// Tracks user behavior and value metrics
/// </summary>
public class UserStatistics
{
    /// <summary>
    /// Total number of orders placed by user
    /// Key metric for customer segmentation
    /// </summary>
    [BsonElement("totalOrders")]
    public int TotalOrders { get; set; } = 0;

    /// <summary>
    /// Total number of photos uploaded by user
    /// Indicates engagement level
    /// </summary>
    [BsonElement("totalPhotosUploaded")]
    public int TotalPhotosUploaded { get; set; } = 0;

    /// <summary>
    /// Total dollar amount spent across all orders
    /// Critical for customer lifetime value calculations
    /// </summary>
    [BsonElement("lifetimeValue")]
    public decimal LifetimeValue { get; set; } = 0;

    /// <summary>
    /// Average order value for personalization
    /// Helps with upselling and recommendations
    /// </summary>
    [BsonElement("averageOrderValue")]
    public decimal AverageOrderValue { get; set; } = 0;

    /// <summary>
    /// Date of most recent order
    /// Used for customer reactivation campaigns
    /// </summary>
    [BsonElement("lastOrderDate")]
    public DateTime? LastOrderDate { get; set; }

    /// <summary>
    /// User's most frequently ordered print size
    /// Used for personalized defaults and recommendations
    /// </summary>
    [BsonElement("favoriteSize")]
    public string? FavoriteSize { get; set; }
}