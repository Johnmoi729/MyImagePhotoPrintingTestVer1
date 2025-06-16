using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.Entities;

/// <summary>
/// Technical image data extracted during processing
/// Critical for print quality validation and sizing
/// </summary>
public class PhotoImageData
{
    /// <summary>
    /// Image width in pixels
    /// </summary>
    [BsonElement("width")]
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    [BsonElement("height")]
    public int Height { get; set; }

    /// <summary>
    /// Image orientation (landscape, portrait, square)
    /// Affects print layout and cropping suggestions
    /// </summary>
    [BsonElement("orientation")]
    public string Orientation { get; set; } = string.Empty;

    /// <summary>
    /// Aspect ratio as string (4:3, 16:9, etc.)
    /// Helps match photos to appropriate print sizes
    /// </summary>
    [BsonElement("aspectRatio")]
    public string AspectRatio { get; set; } = string.Empty;

    /// <summary>
    /// Dots per inch for print quality calculation
    /// Higher DPI generally means better print quality
    /// </summary>
    [BsonElement("dpi")]
    public int Dpi { get; set; } = 72;

    /// <summary>
    /// Color space information (sRGB, Adobe RGB, etc.)
    /// Important for accurate color reproduction in prints
    /// </summary>
    [BsonElement("colorSpace")]
    public string ColorSpace { get; set; } = "sRGB";

    /// <summary>
    /// Whether image has transparency (PNG alpha channel)
    /// Affects printing options and background handling
    /// </summary>
    [BsonElement("hasTransparency")]
    public bool HasTransparency { get; set; } = false;
}