using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Processing status and background job tracking
/// </summary>
public class PhotoProcessing
{
    /// <summary>
    /// Current processing status (uploaded, processing, completed, failed)
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "uploaded";

    /// <summary>
    /// Whether thumbnail generation is complete
    /// </summary>
    [BsonElement("thumbnailGenerated")]
    public bool ThumbnailGenerated { get; set; } = false;

    /// <summary>
    /// Whether AI enhancement is available for this photo
    /// </summary>
    [BsonElement("aiEnhancementAvailable")]
    public bool AiEnhancementAvailable { get; set; } = false;

    /// <summary>
    /// Any errors that occurred during processing
    /// </summary>
    [BsonElement("processingErrors")]
    public List<string> ProcessingErrors { get; set; } = new();

    /// <summary>
    /// When processing was completed
    /// </summary>
    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; set; }
}