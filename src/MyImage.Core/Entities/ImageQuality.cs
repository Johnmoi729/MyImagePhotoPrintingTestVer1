using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Image quality analysis
/// </summary>
public class ImageQuality
{
    [BsonElement("score")]
    public double Score { get; set; }

    [BsonElement("sharpness")]
    public string Sharpness { get; set; } = string.Empty;

    [BsonElement("exposure")]
    public string Exposure { get; set; } = string.Empty;

    [BsonElement("suggestions")]
    public List<string> Suggestions { get; set; } = new();
}