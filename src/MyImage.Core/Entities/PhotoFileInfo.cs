using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// File information embedded within Photo document
/// Contains original upload details and file characteristics
/// </summary>
public class PhotoFileInfo
{
    /// <summary>
    /// Original filename as uploaded by user
    /// Preserved for user recognition and download
    /// </summary>
    [BsonElement("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Sanitized filename for safe storage
    /// Removes special characters and ensures uniqueness
    /// </summary>
    [BsonElement("sanitizedFilename")]
    public string SanitizedFilename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes for storage management
    /// Used for quota tracking and upload validation
    /// </summary>
    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the uploaded file
    /// Ensures proper handling of different image formats
    /// </summary>
    [BsonElement("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// When the file was uploaded to our system
    /// </summary>
    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}