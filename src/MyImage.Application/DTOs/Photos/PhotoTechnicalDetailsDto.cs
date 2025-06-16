using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for technical image details.
/// Contains technical specifications important for print quality assessment.
/// </summary>
public class PhotoTechnicalDetailsDto
{
    /// <summary>
    /// Image resolution in dots per inch.
    /// Higher DPI generally means better print quality, especially for larger sizes.
    /// </summary>
    public int Dpi { get; set; }

    /// <summary>
    /// Color space of the image (sRGB, Adobe RGB, etc.).
    /// Important for accurate color reproduction in prints.
    /// </summary>
    public string ColorSpace { get; set; } = string.Empty;

    /// <summary>
    /// Whether the image contains transparency information.
    /// Affects print options and background handling during processing.
    /// </summary>
    public bool HasTransparency { get; set; }

    /// <summary>
    /// Overall quality score from 1-10.
    /// Helps users understand whether their photo is suitable for printing at different sizes.
    /// </summary>
    public double QualityScore { get; set; }
}