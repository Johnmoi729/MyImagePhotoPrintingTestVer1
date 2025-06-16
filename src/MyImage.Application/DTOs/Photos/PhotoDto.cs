using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object representing a photo in API responses.
/// This is the main DTO for displaying photo information in the user interface.
/// Contains all the essential information needed for photo gallery display and basic management.
/// Excludes sensitive internal data while providing everything the frontend needs.
/// </summary>
public class PhotoDto
{
    /// <summary>
    /// Unique identifier for this photo across the entire system.
    /// Used in API endpoints, URL routing, and as a reference for operations like ordering or editing.
    /// Frontend stores this ID to perform actions on specific photos.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The original filename as uploaded by the user.
    /// Displayed in the UI to help users identify their photos, especially when they have many similar images.
    /// Preserves the user's original naming convention for familiarity and recognition.
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Original file size in bytes for user information and storage management.
    /// Displayed to users as formatted file size (MB, KB) to help them understand storage usage.
    /// Also used by the frontend to show upload progress and validate file size limits.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When this photo was uploaded to our system.
    /// Used for chronological sorting in photo galleries and for user reference.
    /// Helps users organize photos by upload date, which often correlates with when they worked on editing projects.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// URL to the thumbnail version of this photo.
    /// Critical for gallery performance - allows fast loading of photo grids without downloading full-size images.
    /// Thumbnail is generated during photo processing and stored in our CDN or storage system.
    /// </summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to a medium-sized version suitable for preview and detail views.
    /// Provides better quality than thumbnails for photo detail pages without the bandwidth cost of full resolution.
    /// Used when users click on photos to see larger previews before deciding to print.
    /// </summary>
    public string PreviewUrl { get; set; } = string.Empty;

    /// <summary>
    /// Physical dimensions of the original image.
    /// Essential for determining what print sizes are appropriate for this photo.
    /// Helps users understand the quality and aspect ratio limitations for different print formats.
    /// </summary>
    public PhotoDimensionsDto Dimensions { get; set; } = new();

    /// <summary>
    /// User-defined tags associated with this photo.
    /// Enables search and filtering functionality in the photo gallery.
    /// Allows users to quickly find photos by category, event, or any other organizational system they prefer.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// User's personal notes about this photo.
    /// Provides context and memories associated with the image.
    /// Helps users remember details about when and where the photo was taken.
    /// </summary>
    public string? UserNotes { get; set; }

    /// <summary>
    /// Current processing status of this photo.
    /// Informs users whether their photo is ready for printing or still being processed.
    /// Values include "uploaded", "processing", "completed", "failed" to show progress through our processing pipeline.
    /// </summary>
    public string ProcessingStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether this photo has been marked as a favorite by the user.
    /// Enables users to quickly access their most important or frequently used photos.
    /// Favorites often appear at the top of galleries or in special favorite collections.
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Number of times this photo has been printed.
    /// Helps users identify their most popular photos and enables reorder functionality.
    /// Business intelligence data that can also help with recommendations and user engagement tracking.
    /// </summary>
    public int PrintCount { get; set; } = 0;
}