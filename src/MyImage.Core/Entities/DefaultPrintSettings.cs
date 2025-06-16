using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Default print settings to speed up ordering process
/// Pre-populates form fields with user's typical choices
/// </summary>
public class DefaultPrintSettings
{
    /// <summary>
    /// Default print size (4x6, 8x10, etc.)
    /// Based on user's most common orders
    /// </summary>
    [BsonElement("size")]
    public string Size { get; set; } = "4x6";

    /// <summary>
    /// Default finish preference (glossy, matte, pearl)
    /// </summary>
    [BsonElement("finish")]
    public string Finish { get; set; } = "glossy";

    /// <summary>
    /// Default quantity per photo
    /// </summary>
    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;
}