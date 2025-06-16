using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Storage information for photo files across different providers
/// Supports multiple storage backends with consistent access patterns
/// </summary>
public class PhotoStorage
{
    /// <summary>
    /// Storage provider type (GridFS, Azure, local)
    /// Determines how to access and manage the files
    /// </summary>
    [BsonElement("provider")]
    public string Provider { get; set; } = "GridFS";

    /// <summary>
    /// Storage container or bucket name
    /// Organizes files within the storage system
    /// </summary>
    [BsonElement("container")]
    public string Container { get; set; } = "photos";

    /// <summary>
    /// File paths for different image sizes
    /// Original, processed versions, and thumbnails
    /// </summary>
    [BsonElement("paths")]
    public Dictionary<string, string> Paths { get; set; } = new();

    /// <summary>
    /// Public URLs for accessing images
    /// CDN or direct access URLs for each image size
    /// </summary>
    [BsonElement("urls")]
    public Dictionary<string, string> Urls { get; set; } = new();
}