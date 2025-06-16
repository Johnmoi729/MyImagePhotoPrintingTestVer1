using MyImage.Core.Entities;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for Photo entity operations.
/// Handles all database operations related to user-uploaded photos.
/// This interface abstracts the data access layer for photo management,
/// allowing the application to work with photos without knowing MongoDB implementation details.
/// </summary>
public interface IPhotoRepository
{
    /// <summary>
    /// Retrieves a photo by its unique identifier.
    /// Used for displaying photo details, updating metadata, and processing operations.
    /// Should include all embedded documents (file info, storage, EXIF, etc.).
    /// </summary>
    /// <param name="id">Photo's MongoDB ObjectId as string</param>
    /// <returns>Photo entity or null if not found</returns>
    Task<Photo?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves a photo by ID, but only if it belongs to the specified user.
    /// Critical security method - ensures users can only access their own photos.
    /// Used in all user-facing photo operations to prevent unauthorized access.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="userId">User's unique identifier for ownership verification</param>
    /// <returns>Photo entity or null if not found or access denied</returns>
    Task<Photo?> GetByIdAndUserAsync(string id, string userId);

    /// <summary>
    /// Retrieves a paginated list of photos for a specific user.
    /// Core method for photo gallery functionality with filtering and sorting support.
    /// Supports search by tags, date ranges, and other criteria.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="skip">Number of photos to skip for pagination</param>
    /// <param name="take">Number of photos to retrieve (page size)</param>
    /// <param name="sortBy">Field to sort by (uploadDate, filename, size)</param>
    /// <param name="sortOrder">Sort direction (asc or desc)</param>
    /// <param name="searchTerm">Optional search term for filename and tags</param>
    /// <param name="tags">Optional tag filter (comma-separated)</param>
    /// <param name="dateFrom">Optional start date filter</param>
    /// <param name="dateTo">Optional end date filter</param>
    /// <returns>Paginated collection of user's photos</returns>
    Task<IEnumerable<Photo>> GetUserPhotosAsync(
        string userId,
        int skip = 0,
        int take = 20,
        string sortBy = "uploadDate",
        string sortOrder = "desc",
        string? searchTerm = null,
        string? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    /// <summary>
    /// Gets the total count of photos for a user.
    /// Used for pagination calculations and user statistics.
    /// Applies the same filters as GetUserPhotosAsync for accurate counts.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="searchTerm">Optional search term to match the gallery query</param>
    /// <param name="tags">Optional tag filter to match the gallery query</param>
    /// <param name="dateFrom">Optional start date filter</param>
    /// <param name="dateTo">Optional end date filter</param>
    /// <returns>Total number of photos matching the criteria</returns>
    Task<long> GetUserPhotoCountAsync(
        string userId,
        string? searchTerm = null,
        string? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    /// <summary>
    /// Creates a new photo record in the database.
    /// Called after successful file upload and initial processing.
    /// Should generate a new ObjectId and set creation timestamps.
    /// </summary>
    /// <param name="photo">Photo entity with all required information populated</param>
    /// <returns>Created photo entity with MongoDB-generated ID</returns>
    Task<Photo> CreateAsync(Photo photo);

    /// <summary>
    /// Updates an existing photo's metadata and settings.
    /// Used for tag updates, note changes, processing status updates, and AI analysis results.
    /// Should automatically update the UpdatedAt timestamp.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="photo">Updated photo entity with new values</param>
    /// <returns>True if update succeeded, false if photo not found</returns>
    Task<bool> UpdateAsync(string id, Photo photo);

    /// <summary>
    /// Performs a soft delete of a photo.
    /// Sets the IsDeleted flag to true rather than removing the record entirely.
    /// Preserves print history and order relationships while hiding from user interface.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <returns>True if deletion succeeded, false if photo not found</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Updates the processing status of a photo.
    /// Called by background jobs during thumbnail generation, format conversion, and AI analysis.
    /// Allows tracking of photo processing pipeline progress.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="status">New processing status (processing, completed, failed)</param>
    /// <param name="errors">Optional error messages if processing failed</param>
    /// <returns>True if update succeeded</returns>
    Task<bool> UpdateProcessingStatusAsync(string id, string status, List<string>? errors = null);

    /// <summary>
    /// Retrieves photos that need processing.
    /// Used by background jobs to find uploaded photos that haven't been processed yet.
    /// Helps maintain the photo processing pipeline and handle failed processing retries.
    /// </summary>
    /// <param name="status">Processing status to filter by (uploaded, processing)</param>
    /// <param name="limit">Maximum number of photos to retrieve for batch processing</param>
    /// <returns>Collection of photos needing processing</returns>
    Task<IEnumerable<Photo>> GetPhotosForProcessingAsync(string status = "uploaded", int limit = 10);

    /// <summary>
    /// Performs bulk updates on multiple photos.
    /// Used for batch operations like adding tags, changing privacy settings, or bulk deletion.
    /// More efficient than individual updates when modifying many photos at once.
    /// </summary>
    /// <param name="photoIds">Collection of photo IDs to update</param>
    /// <param name="updates">Dictionary of field updates to apply</param>
    /// <param name="userId">User ID for security verification - ensures user owns all photos</param>
    /// <returns>Number of photos successfully updated</returns>
    Task<int> BulkUpdateAsync(IEnumerable<string> photoIds, Dictionary<string, object> updates, string userId);

    /// <summary>
    /// Adds a print record to a photo's history.
    /// Called when a photo is included in a completed order.
    /// Tracks how many times and in what sizes photos have been printed.
    /// </summary>
    /// <param name="photoId">Photo's unique identifier</param>
    /// <param name="printRecord">Details of the print job (order ID, size, quantity)</param>
    /// <returns>True if record was added successfully</returns>
    Task<bool> AddPrintRecordAsync(string photoId, PhotoPrintRecord printRecord);
}