using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// EXIF metadata extracted from image files
/// Camera settings and technical details from photography
/// </summary>
public class PhotoExifData
{
    /// <summary>
    /// Camera information embedded document
    /// </summary>
    [BsonElement("camera")]
    public CameraInfo? Camera { get; set; }

    /// <summary>
    /// Photo settings when image was captured
    /// </summary>
    [BsonElement("settings")]
    public CameraSettings? Settings { get; set; }

    /// <summary>
    /// When the photo was actually taken (from EXIF)
    /// May differ from upload time - important for photo organization
    /// </summary>
    [BsonElement("timestamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// GPS coordinates if location services were enabled
    /// Privacy-sensitive data - handle with care
    /// </summary>
    [BsonElement("gps")]
    public GpsCoordinates? Gps { get; set; }
}