using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Photo status flags for management and filtering
/// </summary>
public class PhotoFlags
{
    /// <summary>
    /// Soft delete flag - photo is hidden but not removed
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// User-marked favorite for easy access
    /// </summary>
    [BsonElement("isFavorite")]
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Private photo - not visible in shared galleries
    /// </summary>
    [BsonElement("isPrivate")]
    public bool IsPrivate { get; set; } = false;

    /// <summary>
    /// Whether photo has been reported for inappropriate content
    /// </summary>
    [BsonElement("reportedContent")]
    public bool ReportedContent { get; set; } = false;
}