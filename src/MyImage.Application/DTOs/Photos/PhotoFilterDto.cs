using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo search and filtering parameters.
/// Used to construct complex queries for photo gallery and search functionality.
/// Supports multiple filter types that can be combined for powerful photo discovery.
/// </summary>
public class PhotoFilterDto
{
    /// <summary>
    /// Text search term to match against filenames and tags.
    /// Uses fuzzy matching to help users find photos even with approximate spelling.
    /// Searches across multiple fields to maximize the chance of finding relevant photos.
    /// </summary>
    [StringLength(100, ErrorMessage = "Search term cannot exceed 100 characters")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Specific tags to filter by - supports multiple tag filtering.
    /// Comma-separated list of tags that photos must have to be included in results.
    /// Can be combined with other filters for precise photo discovery.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Start date for filtering photos by upload date.
    /// Includes photos uploaded on or after this date.
    /// Useful for finding photos from specific time periods or recent uploads.
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// End date for filtering photos by upload date.
    /// Includes photos uploaded on or before this date.
    /// Combined with DateFrom to create date range filters for temporal organization.
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Filter by favorite status to show only favorites or exclude them.
    /// Null means include all photos regardless of favorite status.
    /// True shows only favorites, false shows only non-favorites.
    /// </summary>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Filter by processing status to find photos in specific states.
    /// Useful for finding photos that failed processing or are still being processed.
    /// Helps users understand the status of their photo uploads and identify issues.
    /// </summary>
    public string? ProcessingStatus { get; set; }

    /// <summary>
    /// Field to sort results by - supports multiple sorting options.
    /// Common values include "uploadDate", "filename", "fileSize", "printCount".
    /// Allows users to organize their photo gallery according to their preferences.
    /// </summary>
    public string SortBy { get; set; } = "uploadedAt";

    /// <summary>
    /// Sort direction for the specified sort field.
    /// "asc" for ascending (oldest first, A-Z, smallest first)
    /// "desc" for descending (newest first, Z-A, largest first)
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// Page number for pagination - starts at 1.
    /// Used with PageSize to implement efficient loading of large photo collections.
    /// Enables smooth scrolling experiences and reduces initial load times.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be 1 or greater")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of photos to return per page.
    /// Balances between fewer requests and manageable data sizes.
    /// Typical values range from 10-50 depending on UI design and performance requirements.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}