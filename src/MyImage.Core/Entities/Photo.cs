using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Core Photo entity representing uploaded images ready for printing
/// Central to the photo printing business - contains all image metadata,
/// processing status, and relationship to orders and user interactions
/// </summary>
public class Photo
{
    /// <summary>
    /// MongoDB ObjectId primary key for photo identification
    /// Used in URLs, API calls, and database relationships
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the user who owns this photo
    /// Critical for security - ensures users only see their own photos
    /// </summary>
    [Required]
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// File information embedded document
    /// Contains original filename, size, and upload metadata
    /// </summary>
    [BsonElement("fileInfo")]
    public PhotoFileInfo FileInfo { get; set; } = new();

    /// <summary>
    /// Storage location and URL information
    /// Handles different storage providers (GridFS, Azure, local)
    /// </summary>
    [BsonElement("storage")]
    public PhotoStorage Storage { get; set; } = new();

    /// <summary>
    /// Image technical data extracted during upload
    /// Used for print quality validation and size calculations
    /// </summary>
    [BsonElement("imageData")]
    public PhotoImageData ImageData { get; set; } = new();

    /// <summary>
    /// EXIF data extracted from the image file
    /// Camera settings, GPS location, timestamp information
    /// </summary>
    [BsonElement("exifData")]
    public PhotoExifData? ExifData { get; set; }

    /// <summary>
    /// Processing status and background job tracking
    /// Tracks thumbnail generation, format conversion, quality analysis
    /// </summary>
    [BsonElement("processing")]
    public PhotoProcessing Processing { get; set; } = new();

    /// <summary>
    /// History of times this photo has been printed
    /// Useful for reorder functionality and usage analytics
    /// </summary>
    [BsonElement("printHistory")]
    public List<PhotoPrintRecord> PrintHistory { get; set; } = new();

    /// <summary>
    /// User-defined tags for organization and search
    /// Helps users find photos quickly in large collections
    /// </summary>
    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// User's personal notes about this photo
    /// Memories, context, or printing preferences
    /// </summary>
    [BsonElement("userNotes")]
    public string? UserNotes { get; set; }

    /// <summary>
    /// AI-powered image analysis results (if enabled)
    /// Scene detection, quality scoring, enhancement suggestions
    /// </summary>
    [BsonElement("aiAnalysis")]
    public PhotoAIAnalysis? AIAnalysis { get; set; }

    /// <summary>
    /// Photo status flags for filtering and management
    /// Soft delete, favorites, privacy settings
    /// </summary>
    [BsonElement("flags")]
    public PhotoFlags Flags { get; set; } = new();

    /// <summary>
    /// System metadata for auditing and versioning
    /// Creation time, updates, processing completion
    /// </summary>
    [BsonElement("metadata")]
    public PhotoMetadata Metadata { get; set; } = new();
}