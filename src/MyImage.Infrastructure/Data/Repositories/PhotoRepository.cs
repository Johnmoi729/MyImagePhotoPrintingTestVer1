using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Infrastructure.Data.MongoDb;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// MongoDB implementation of the Photo repository interface.
/// This class provides comprehensive data access methods for photo management using MongoDB as the storage backend.
/// It handles complex photo queries, filtering, sorting, and relationship management while maintaining optimal performance
/// through proper indexing and efficient query patterns. The implementation supports the core photo printing workflow.
/// </summary>
public class PhotoRepository : IPhotoRepository
{
    private readonly IMongoCollection<Photo> _photos;
    private readonly ILogger<PhotoRepository> _logger;

    /// <summary>
    /// Initializes the photo repository with MongoDB context and logging.
    /// Sets up the MongoDB collection reference and configures logging for photo data access operations.
    /// The repository leverages MongoDB's document model to efficiently store and query photo metadata.
    /// </summary>
    /// <param name="mongoContext">MongoDB database context providing access to photo collection</param>
    /// <param name="logger">Logger for tracking photo repository operations and performance monitoring</param>
    public PhotoRepository(MongoDbContext mongoContext, ILogger<PhotoRepository> logger)
    {
        _photos = mongoContext.Photos;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a photo by its unique MongoDB ObjectId.
    /// This method provides fast photo lookup using the primary key index and returns
    /// the complete photo document including all metadata, processing status, and relationships.
    /// Used for photo detail views and administrative operations.
    /// </summary>
    /// <param name="id">Photo's MongoDB ObjectId as string</param>
    /// <returns>Complete photo entity or null if not found</returns>
    public async Task<Photo?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Retrieving photo by ID: {PhotoId}", id);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, id);
            var photo = await _photos.Find(filter).FirstOrDefaultAsync();

            if (photo != null)
            {
                _logger.LogDebug("Photo found: {PhotoId}, Filename: {Filename}", photo.Id, photo.FileInfo.OriginalFilename);
            }
            else
            {
                _logger.LogDebug("Photo not found for ID: {PhotoId}", id);
            }

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo by ID: {PhotoId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a photo by ID with user ownership verification for security.
    /// This method ensures users can only access their own photos by combining photo ID lookup
    /// with user ownership validation. Critical for maintaining data security in multi-user environments.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="userId">User's unique identifier for ownership verification</param>
    /// <returns>Photo entity if found and owned by user, null otherwise</returns>
    public async Task<Photo?> GetByIdAndUserAsync(string id, string userId)
    {
        try
        {
            _logger.LogDebug("Retrieving photo by ID and user: PhotoId={PhotoId}, UserId={UserId}", id, userId);

            // Combine photo ID and user ownership filters for security
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.Eq(p => p.Id, id),
                Builders<Photo>.Filter.Eq(p => p.UserId, userId),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            var photo = await _photos.Find(filter).FirstOrDefaultAsync();

            if (photo != null)
            {
                _logger.LogDebug("Photo found for user: {PhotoId}", id);
            }
            else
            {
                _logger.LogDebug("Photo not found or access denied: PhotoId={PhotoId}, UserId={UserId}", id, userId);
            }

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo by ID and user: PhotoId={PhotoId}, UserId={UserId}", id, userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a paginated and filtered collection of photos for a specific user.
    /// This method implements the core photo gallery functionality with comprehensive filtering,
    /// searching, and sorting capabilities. It uses MongoDB's efficient aggregation pipeline
    /// and indexes to deliver fast performance even with large photo collections.
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
    /// <returns>Paginated collection of user's photos matching the criteria</returns>
    public async Task<IEnumerable<Photo>> GetUserPhotosAsync(
        string userId,
        int skip = 0,
        int take = 20,
        string sortBy = "uploadDate",
        string sortOrder = "desc",
        string? searchTerm = null,
        string? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogDebug("Retrieving user photos: UserId={UserId}, Skip={Skip}, Take={Take}, SortBy={SortBy}, SortOrder={SortOrder}",
                userId, skip, take, sortBy, sortOrder);

            // Build the base filter for user ownership and non-deleted photos
            var filterBuilder = Builders<Photo>.Filter;
            var filters = new List<FilterDefinition<Photo>>
            {
                filterBuilder.Eq(p => p.UserId, userId),
                filterBuilder.Eq("flags.isDeleted", false)
            };

            // Add search term filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex("fileInfo.originalFilename", new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(p => p.UserNotes, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.AnyEq(p => p.Tags, searchTerm)
                );
                filters.Add(searchFilter);
            }

            // Add tag filters if provided
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToList();

                if (tagList.Any())
                {
                    var tagFilters = tagList.Select(tag => filterBuilder.AnyEq(p => p.Tags, tag));
                    filters.Add(filterBuilder.And(tagFilters));
                }
            }

            // Add date range filters if provided
            if (dateFrom.HasValue)
            {
                filters.Add(filterBuilder.Gte("fileInfo.uploadedAt", dateFrom.Value));
            }

            if (dateTo.HasValue)
            {
                filters.Add(filterBuilder.Lte("fileInfo.uploadedAt", dateTo.Value.AddDays(1))); // Include full day
            }

            // Combine all filters
            var combinedFilter = filterBuilder.And(filters);

            // Build sort definition based on parameters
            var sortDefinition = BuildSortDefinition(sortBy, sortOrder);

            // Execute the query with pagination
            var photos = await _photos
                .Find(combinedFilter)
                .Sort(sortDefinition)
                .Skip(skip)
                .Limit(take)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} photos for user: {UserId}", photos.Count, userId);

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user photos: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of photos for a user matching the specified criteria.
    /// This method applies the same filters as GetUserPhotosAsync to provide accurate
    /// pagination information. It's optimized for counting operations using MongoDB's countDocuments.
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="searchTerm">Optional search term to match the gallery query</param>
    /// <param name="tags">Optional tag filter to match the gallery query</param>
    /// <param name="dateFrom">Optional start date filter</param>
    /// <param name="dateTo">Optional end date filter</param>
    /// <returns>Total number of photos matching the criteria</returns>
    public async Task<long> GetUserPhotoCountAsync(
        string userId,
        string? searchTerm = null,
        string? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        try
        {
            _logger.LogDebug("Counting user photos: {UserId}", userId);

            // Build the same filter logic as GetUserPhotosAsync for consistency
            var filterBuilder = Builders<Photo>.Filter;
            var filters = new List<FilterDefinition<Photo>>
            {
                filterBuilder.Eq(p => p.UserId, userId),
                filterBuilder.Eq("flags.isDeleted", false)
            };

            // Apply the same search and filter logic
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex("fileInfo.originalFilename", new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(p => p.UserNotes, new BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.AnyEq(p => p.Tags, searchTerm)
                );
                filters.Add(searchFilter);
            }

            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToList();

                if (tagList.Any())
                {
                    var tagFilters = tagList.Select(tag => filterBuilder.AnyEq(p => p.Tags, tag));
                    filters.Add(filterBuilder.And(tagFilters));
                }
            }

            if (dateFrom.HasValue)
            {
                filters.Add(filterBuilder.Gte("fileInfo.uploadedAt", dateFrom.Value));
            }

            if (dateTo.HasValue)
            {
                filters.Add(filterBuilder.Lte("fileInfo.uploadedAt", dateTo.Value.AddDays(1)));
            }

            var combinedFilter = filterBuilder.And(filters);
            var count = await _photos.CountDocumentsAsync(combinedFilter);

            _logger.LogDebug("Photo count for user {UserId}: {Count}", userId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting user photos: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new photo record in the database after successful upload and processing.
    /// This method inserts a complete photo document with all metadata, storage information,
    /// and initial processing status. MongoDB automatically generates a unique ObjectId.
    /// </summary>
    /// <param name="photo">Complete photo entity with all upload information</param>
    /// <returns>Created photo entity with MongoDB-generated ID</returns>
    public async Task<Photo> CreateAsync(Photo photo)
    {
        try
        {
            _logger.LogDebug("Creating new photo: {Filename}, UserId: {UserId}",
                photo.FileInfo.OriginalFilename, photo.UserId);

            // Set creation and update timestamps
            photo.Metadata.CreatedAt = DateTime.UtcNow;
            photo.Metadata.UpdatedAt = DateTime.UtcNow;

            // Ensure processing status is set
            if (string.IsNullOrEmpty(photo.Processing.Status))
            {
                photo.Processing.Status = "uploaded";
            }

            // Insert the photo document
            await _photos.InsertOneAsync(photo);

            _logger.LogInformation("Photo created successfully: {PhotoId}, Filename: {Filename}, UserId: {UserId}",
                photo.Id, photo.FileInfo.OriginalFilename, photo.UserId);

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating photo: {Filename}, UserId: {UserId}",
                photo.FileInfo.OriginalFilename, photo.UserId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing photo's metadata and processing information.
    /// This method performs a complete document replacement while preserving essential fields
    /// like ID and creation date. It automatically updates the modification timestamp.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="photo">Updated photo entity with new values</param>
    /// <returns>True if update succeeded, false if photo not found</returns>
    public async Task<bool> UpdateAsync(string id, Photo photo)
    {
        try
        {
            _logger.LogDebug("Updating photo: {PhotoId}", id);

            // Preserve ID and update timestamp
            photo.Id = id;
            photo.Metadata.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, id);
            var result = await _photos.ReplaceOneAsync(filter, photo);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("Photo updated successfully: {PhotoId}", id);
            }
            else
            {
                _logger.LogWarning("Photo update failed - photo not found: {PhotoId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo: {PhotoId}", id);
            throw;
        }
    }

    /// <summary>
    /// Performs a soft delete of a photo by setting the deletion flag.
    /// This method preserves photo data for referential integrity with orders and print history
    /// while removing it from user interfaces. Soft deletion is preferred for business data preservation.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <returns>True if deletion succeeded, false if photo not found</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            _logger.LogDebug("Soft deleting photo: {PhotoId}", id);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, id);
            var update = Builders<Photo>.Update
                .Set("flags.isDeleted", true)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _photos.UpdateOneAsync(filter, update);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogInformation("Photo soft deleted successfully: {PhotoId}", id);
            }
            else
            {
                _logger.LogWarning("Photo soft deletion failed - photo not found: {PhotoId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting photo: {PhotoId}", id);
            throw;
        }
    }

    /// <summary>
    /// Updates the processing status of a photo during background processing operations.
    /// This method efficiently updates only the processing-related fields without affecting
    /// other photo data. Used by background jobs to track photo processing pipeline progress.
    /// </summary>
    /// <param name="id">Photo's unique identifier</param>
    /// <param name="status">New processing status (processing, completed, failed)</param>
    /// <param name="errors">Optional error messages if processing failed</param>
    /// <returns>True if status update succeeded</returns>
    public async Task<bool> UpdateProcessingStatusAsync(string id, string status, List<string>? errors = null)
    {
        try
        {
            _logger.LogDebug("Updating processing status for photo: {PhotoId}, Status: {Status}", id, status);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, id);
            var updateBuilder = Builders<Photo>.Update
                .Set("processing.status", status)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            // Set processing completion time if status is completed
            if (status == "completed")
            {
                updateBuilder = updateBuilder.Set("processing.processedAt", DateTime.UtcNow);
            }

            // Add error messages if provided
            if (errors != null && errors.Any())
            {
                updateBuilder = updateBuilder.Set("processing.processingErrors", errors);
            }

            var result = await _photos.UpdateOneAsync(filter, updateBuilder);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("Processing status updated for photo: {PhotoId}", id);
            }
            else
            {
                _logger.LogWarning("Processing status update failed - photo not found: {PhotoId}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processing status for photo: {PhotoId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves photos that need processing for background job operations.
    /// This method finds photos in specific processing states to enable efficient
    /// batch processing and retry logic for failed operations. Supports the photo processing pipeline.
    /// </summary>
    /// <param name="status">Processing status to filter by (uploaded, processing)</param>
    /// <param name="limit">Maximum number of photos to retrieve for batch processing</param>
    /// <returns>Collection of photos needing processing</returns>
    public async Task<IEnumerable<Photo>> GetPhotosForProcessingAsync(string status = "uploaded", int limit = 10)
    {
        try
        {
            _logger.LogDebug("Retrieving photos for processing: Status={Status}, Limit={Limit}", status, limit);

            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.Eq("processing.status", status),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            // Sort by upload date to process oldest photos first
            var sort = Builders<Photo>.Sort.Ascending("fileInfo.uploadedAt");

            var photos = await _photos
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();

            _logger.LogDebug("Found {Count} photos for processing with status: {Status}", photos.Count, status);

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for processing: Status={Status}", status);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk updates on multiple photos for efficiency.
    /// This method enables operations like bulk tagging, privacy changes, or status updates
    /// across multiple photos simultaneously with user ownership verification for security.
    /// </summary>
    /// <param name="photoIds">Collection of photo IDs to update</param>
    /// <param name="updates">Dictionary of field updates to apply</param>
    /// <param name="userId">User ID for security verification</param>
    /// <returns>Number of photos successfully updated</returns>
    public async Task<int> BulkUpdateAsync(IEnumerable<string> photoIds, Dictionary<string, object> updates, string userId)
    {
        try
        {
            var photoIdList = photoIds.ToList();
            _logger.LogDebug("Performing bulk update on {Count} photos for user: {UserId}", photoIdList.Count, userId);

            // Build filter to include only user's non-deleted photos
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.In(p => p.Id, photoIdList),
                Builders<Photo>.Filter.Eq(p => p.UserId, userId),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            // Build update operations
            var updateBuilder = Builders<Photo>.Update.Set("metadata.updatedAt", DateTime.UtcNow);

            foreach (var update in updates)
            {
                updateBuilder = updateBuilder.Set(update.Key, update.Value);
            }

            var result = await _photos.UpdateManyAsync(filter, updateBuilder);

            _logger.LogDebug("Bulk update completed: {ModifiedCount} photos updated for user: {UserId}",
                result.ModifiedCount, userId);

            return (int)result.ModifiedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk update for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Adds a print record to a photo's history when included in an order.
    /// This method tracks printing activity for analytics, reorder functionality,
    /// and user engagement metrics. Print history is essential for business intelligence.
    /// </summary>
    /// <param name="photoId">Photo's unique identifier</param>
    /// <param name="printRecord">Details of the print job</param>
    /// <returns>True if print record was added successfully</returns>
    public async Task<bool> AddPrintRecordAsync(string photoId, PhotoPrintRecord printRecord)
    {
        try
        {
            _logger.LogDebug("Adding print record to photo: {PhotoId}, OrderId: {OrderId}",
                photoId, printRecord.OrderId);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, photoId);
            var update = Builders<Photo>.Update
                .Push(p => p.PrintHistory, printRecord)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _photos.UpdateOneAsync(filter, update);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("Print record added to photo: {PhotoId}", photoId);
            }
            else
            {
                _logger.LogWarning("Print record addition failed - photo not found: {PhotoId}", photoId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding print record to photo: {PhotoId}", photoId);
            throw;
        }
    }

    /// <summary>
    /// Builds MongoDB sort definition based on sort criteria.
    /// This helper method translates user-friendly sort parameters into MongoDB sort definitions
    /// while providing sensible defaults and handling edge cases.
    /// </summary>
    /// <param name="sortBy">Field to sort by</param>
    /// <param name="sortOrder">Sort direction</param>
    /// <returns>MongoDB sort definition</returns>
    private SortDefinition<Photo> BuildSortDefinition(string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "filename" => isDescending
                ? Builders<Photo>.Sort.Descending("fileInfo.originalFilename")
                : Builders<Photo>.Sort.Ascending("fileInfo.originalFilename"),
            "filesize" => isDescending
                ? Builders<Photo>.Sort.Descending("fileInfo.fileSize")
                : Builders<Photo>.Sort.Ascending("fileInfo.fileSize"),
            "printcount" => isDescending
                ? Builders<Photo>.Sort.Descending(p => p.PrintHistory.Count)
                : Builders<Photo>.Sort.Ascending(p => p.PrintHistory.Count),
            "processingstatus" => isDescending
                ? Builders<Photo>.Sort.Descending("processing.status")
                : Builders<Photo>.Sort.Ascending("processing.status"),
            "uploaddate" or "uploaded" or _ => isDescending
                ? Builders<Photo>.Sort.Descending("fileInfo.uploadedAt")
                : Builders<Photo>.Sort.Ascending("fileInfo.uploadedAt")
        };
    }

    /// <summary>
    /// Updates AI analysis results for a photo after processing completion.
    /// This method stores machine learning analysis results including scene detection,
    /// quality assessment, and enhancement recommendations for improved user experience.
    /// </summary>
    /// <param name="photoId">Photo's unique identifier</param>
    /// <param name="aiAnalysis">AI analysis results to store</param>
    /// <returns>True if AI analysis was stored successfully</returns>
    public async Task<bool> UpdateAIAnalysisAsync(string photoId, PhotoAIAnalysis aiAnalysis)
    {
        try
        {
            _logger.LogDebug("Updating AI analysis for photo: {PhotoId}", photoId);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, photoId);
            var update = Builders<Photo>.Update
                .Set(p => p.AIAnalysis, aiAnalysis)
                .Set("processing.aiEnhancementAvailable", true)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _photos.UpdateOneAsync(filter, update);

            var success = result.ModifiedCount > 0;

            if (success)
            {
                _logger.LogDebug("AI analysis updated for photo: {PhotoId}", photoId);
            }
            else
            {
                _logger.LogWarning("AI analysis update failed - photo not found: {PhotoId}", photoId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI analysis for photo: {PhotoId}", photoId);
            throw;
        }
    }
}