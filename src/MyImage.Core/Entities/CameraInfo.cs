using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Camera information from EXIF data
/// </summary>
public class CameraInfo
{
    [BsonElement("make")]
    public string? Make { get; set; }

    [BsonElement("model")]
    public string? Model { get; set; }

    [BsonElement("lens")]
    public string? Lens { get; set; }
}