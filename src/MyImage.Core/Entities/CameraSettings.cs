using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Camera settings when photo was captured
/// </summary>
public class CameraSettings
{
    [BsonElement("iso")]
    public int? Iso { get; set; }

    [BsonElement("aperture")]
    public string? Aperture { get; set; }

    [BsonElement("shutterSpeed")]
    public string? ShutterSpeed { get; set; }

    [BsonElement("focalLength")]
    public string? FocalLength { get; set; }
}