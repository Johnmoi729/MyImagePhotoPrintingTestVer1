using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo update operations.
/// Used when users modify photo metadata such as tags, notes, or privacy settings.
/// Allows partial updates without requiring users to re-upload or reprocess photos.
/// </summary>
public class PhotoUpdateDto
{
    /// <summary>
    /// Updated tags for photo organization.
    /// Replaces existing tags entirely - frontend should send all desired tags, not just additions.
    /// Comma-separated string that will be parsed and validated during processing.
    /// </summary>
    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters total")]
    public string? Tags { get; set; }

    /// <summary>
    /// Updated user notes or description.
    /// Replaces existing notes with new content - supports markdown formatting if implemented.
    /// Allows users to add more context or update information as memories develop.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? UserNotes { get; set; }

    /// <summary>
    /// Updated favorite status for this photo.
    /// Simple boolean toggle that can be changed frequently as user preferences evolve.
    /// Used to build favorite collections and prioritize photos in gallery displays.
    /// </summary>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Updated privacy setting for this photo.
    /// Allows users to change privacy levels as their comfort and needs change over time.
    /// Important for photos that might start as private but later become suitable for sharing.
    /// </summary>
    public bool? IsPrivate { get; set; }
}