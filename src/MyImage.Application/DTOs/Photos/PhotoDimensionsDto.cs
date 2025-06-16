using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo dimensions and aspect ratio information.
/// Provides essential sizing information for print recommendations and UI display.
/// </summary>
public class PhotoDimensionsDto
{
    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Aspect ratio as a formatted string (e.g., "4:3", "16:9").
    /// Helps users understand cropping requirements for different print sizes.
    /// </summary>
    public string AspectRatio { get; set; } = string.Empty;

    /// <summary>
    /// Image orientation classification.
    /// Values: "landscape", "portrait", "square"
    /// Affects print layout options and recommended sizes.
    /// </summary>
    public string Orientation { get; set; } = string.Empty;
}