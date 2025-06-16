namespace MyImage.Application.DTOs.Common;

/// <summary>
/// Generic paginated result wrapper for collections.
/// This DTO provides consistent pagination information across all paginated endpoints
/// in the application, enabling efficient data loading and navigation for large datasets.
/// </summary>
/// <typeparam name="T">Type of items in the paginated collection</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Collection of items for the current page.
    /// Contains the actual data requested by the client for display.
    /// May be empty if no items match the criteria or page is beyond available data.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Total number of items available across all pages.
    /// Used for calculating pagination information and showing total counts to users.
    /// Essential for frontend pagination controls and progress indicators.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// Indicates which page of data is currently being returned.
    /// Used by frontend pagination controls for navigation and display.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// Indicates how many items are included in each page of results.
    /// Helps frontend applications understand pagination structure.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages available.
    /// Calculated property based on TotalCount and PageSize.
    /// Used for pagination controls and navigation logic.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page available.
    /// Convenience property for frontend pagination controls.
    /// Helps determine if "Previous" navigation should be enabled.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page available.
    /// Convenience property for frontend pagination controls.
    /// Helps determine if "Next" navigation should be enabled.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}