using MyImage.Core.Entities;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Interfaces;

/// <summary>
/// Service interface for photo management operations including upload, processing, and organization.
/// This interface encapsulates all business logic related to photo handling in our printing service.
/// It coordinates between file storage, image processing, metadata management, and user organization features.
/// The service handles the complex workflow of photo processing while presenting a clean interface to controllers.
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Handles the complete photo upload workflow including validation, storage, and processing initiation.
    /// This method orchestrates file validation, virus scanning, storage to our file system,
    /// metadata extraction, thumbnail generation, and database record creation.
    /// The process is designed to handle multiple files efficiently while maintaining data integrity.
    /// </summary>
    /// <param name="files">Collection of uploaded image files from the HTTP request</param>
    /// <param name="uploadDto">Metadata and settings for the uploaded photos</param>
    /// <param name="userId">Unique identifier of the user uploading the photos</param>
    /// <returns>Collection of photo DTOs representing the successfully uploaded photos</returns>
    Task<IEnumerable<PhotoDto>> UploadPhotosAsync(IEnumerable<IFormFile> files, PhotoUploadDto uploadDto, string userId);

    /// <summary>
    /// Retrieves a paginated and filtered collection of photos for a specific user.
    /// This method implements the core photo gallery functionality with support for search,
    /// filtering by tags and dates, and sorting by various criteria.
    /// The pagination system ensures good performance even with large photo collections.
    /// </summary>
    /// <param name="userId">Unique identifier of the user whose photos to retrieve</param>
    /// <param name="filter">Filtering, sorting, and pagination parameters</param>
    /// <returns>Paginated result containing photos and metadata for gallery display</returns>
    Task<PagedResultDto<PhotoDto>> GetUserPhotosAsync(string userId, PhotoFilterDto filter);

    /// <summary>
    /// Retrieves detailed information about a specific photo including technical data and history.
    /// This method provides comprehensive photo information for detail views, including EXIF data,
    /// processing history, print records, and AI analysis results when available.
    /// Security verification ensures users can only access their own photos.
    /// </summary>
    /// <param name="photoId">Unique identifier of the photo to retrieve</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Detailed photo information or null if not found or access denied</returns>
    Task<PhotoDetailsDto?> GetPhotoDetailsAsync(string photoId, string userId);

    /// <summary>
    /// Updates photo metadata including tags, notes, and privacy settings.
    /// This method allows users to organize and annotate their photos after upload.
    /// The update process validates user ownership and maintains data integrity
    /// while allowing flexible photo organization and personalization.
    /// </summary>
    /// <param name="photoId">Unique identifier of the photo to update</param>
    /// <param name="updateDto">Updated metadata and settings</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Updated photo information or null if update failed</returns>
    Task<PhotoDto?> UpdatePhotoAsync(string photoId, PhotoUpdateDto updateDto, string userId);

    /// <summary>
    /// Performs a soft delete of a photo, removing it from user interfaces while preserving data.
    /// This method implements soft deletion to maintain referential integrity with orders and print history.
    /// Deleted photos are hidden from galleries but remain in the system for business records
    /// and can potentially be recovered if needed.
    /// </summary>
    /// <param name="photoId">Unique identifier of the photo to delete</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>True if deletion completed successfully</returns>
    Task<bool> DeletePhotoAsync(string photoId, string userId);

    /// <summary>
    /// Generates a secure, temporary download link for accessing the original photo file.
    /// This method creates time-limited URLs that allow users to download their original photos
    /// without exposing permanent file locations. The temporary nature of these links
    /// provides security while enabling legitimate access to user-owned content.
    /// </summary>
    /// <param name="photoId">Unique identifier of the photo to download</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Download information with temporary URL and expiration time</returns>
    Task<PhotoDownloadDto?> GenerateDownloadLinkAsync(string photoId, string userId);

    /// <summary>
    /// Processes multiple photos in a batch operation for efficiency.
    /// This method enables operations like bulk tagging, privacy changes, or deletion
    /// across multiple photos simultaneously. Batch processing improves user experience
    /// and system efficiency when managing large photo collections.
    /// </summary>
    /// <param name="photoIds">Collection of photo identifiers to process</param>
    /// <param name="operation">Type of operation to perform (tag, delete, favorite, etc.)</param>
    /// <param name="operationData">Additional data needed for the operation</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Number of photos successfully processed</returns>
    Task<int> BulkOperationAsync(IEnumerable<string> photoIds, string operation, object operationData, string userId);

    /// <summary>
    /// Retrieves photos that are suitable for a specific print size based on resolution and quality.
    /// This method analyzes photo dimensions and quality metrics to recommend photos
    /// that will produce high-quality prints at the requested size.
    /// Quality recommendations help users avoid disappointing print results.
    /// </summary>
    /// <param name="userId">User identifier for photo access</param>
    /// <param name="printSize">Desired print size for quality assessment</param>
    /// <returns>Collection of photos suitable for the specified print size</returns>
    Task<IEnumerable<PhotoDto>> GetPhotosForPrintSizeAsync(string userId, string printSize);

    /// <summary>
    /// Applies AI-powered enhancements to improve photo quality before printing.
    /// This method utilizes machine learning algorithms to automatically adjust
    /// brightness, contrast, color balance, and sharpness for optimal print results.
    /// Enhanced versions are stored separately, preserving the original files.
    /// </summary>
    /// <param name="photoId">Unique identifier of the photo to enhance</param>
    /// <param name="enhancementType">Type of enhancement to apply</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Information about the enhanced photo version</returns>
    Task<PhotoDto?> ApplyAIEnhancementAsync(string photoId, string enhancementType, string userId);

    /// <summary>
    /// Retrieves the processing status and history for photos that are being processed.
    /// This method provides real-time updates on photo processing progress,
    /// including thumbnail generation, format conversion, quality analysis, and AI processing.
    /// Status tracking helps users understand when their photos will be ready for printing.
    /// </summary>
    /// <param name="userId">User identifier for photo access</param>
    /// <param name="processingStatus">Optional filter for specific processing states</param>
    /// <returns>Collection of photos with their current processing status</returns>
    Task<IEnumerable<PhotoProcessingStatusDto>> GetProcessingStatusAsync(string userId, string? processingStatus = null);
}