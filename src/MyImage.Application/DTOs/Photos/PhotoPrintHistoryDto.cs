using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for print history records.
/// Shows when and how this photo has been printed previously.
/// </summary>
public class PhotoPrintHistoryDto
{
    /// <summary>
    /// Order ID that included this photo.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable order number for user reference.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// When this print order was placed.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Print size that was ordered.
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Quantity that was printed.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Finish type that was used (glossy, matte, etc.).
    /// </summary>
    public string Finish { get; set; } = string.Empty;
}