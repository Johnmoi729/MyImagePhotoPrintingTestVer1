using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for detailed photo information.
/// Used when displaying comprehensive photo details, including technical specifications and processing history.
/// Extends the basic PhotoDto with additional information needed for photo detail pages and advanced operations.
/// </summary>
public class PhotoDetailsDto : PhotoDto
{
    /// <summary>
    /// URL to the full resolution original image.
    /// Used for download functionality and high-quality print processing.
    /// Only provided when user specifically requests full details to avoid unnecessary bandwidth usage.
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the original uploaded file.
    /// Informs frontend about file format for appropriate handling and display options.
    /// Helps determine what operations are available for this specific image format.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Technical image specifications extracted during upload processing.
    /// Includes DPI, color space, and other technical details important for printing quality.
    /// Helps users and system determine optimal print settings for this specific image.
    /// </summary>
    public PhotoTechnicalDetailsDto TechnicalDetails { get; set; } = new();

    /// <summary>
    /// Camera and photography metadata extracted from EXIF data.
    /// Provides photographers with technical information about how the photo was captured.
    /// Includes camera settings, equipment used, and capture timestamp if available.
    /// </summary>
    public PhotoExifDto? ExifData { get; set; }

    /// <summary>
    /// History of all print orders that included this photo.
    /// Enables reorder functionality and helps users track their printing history.
    /// Each record includes order details, print size, quantity, and date for complete tracking.
    /// </summary>
    public List<PhotoPrintHistoryDto> PrintHistory { get; set; } = new();

    /// <summary>
    /// AI-powered analysis results if processing has been completed.
    /// Includes scene detection, quality assessment, and enhancement suggestions.
    /// Used to provide intelligent recommendations for print sizes and enhancement options.
    /// </summary>
    public PhotoAIAnalysisDto? AiAnalysis { get; set; }

    /// <summary>
    /// Available enhancement options for this photo.
    /// Lists AI-powered improvements that can be applied before printing.
    /// Helps users achieve better print quality through automated adjustments.
    /// </summary>
    public List<string> AvailableEnhancements { get; set; } = new();

    /// <summary>
    /// Recommended print sizes based on image resolution and quality analysis.
    /// Guides users toward print options that will produce the best results for this specific image.
    /// Takes into account resolution, aspect ratio, and quality factors to prevent poor-quality prints.
    /// </summary>
    public List<string> RecommendedPrintSizes { get; set; } = new();
}