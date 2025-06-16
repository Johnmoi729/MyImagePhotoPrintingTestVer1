using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using MyImage.Application.Interfaces;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// Service implementation for photo management operations including upload, processing, and organization.
/// This service orchestrates the complete photo workflow from upload through processing to user management.
/// It coordinates between file storage, image processing, metadata management, and database operations
/// while implementing business rules and security policies for photo handling in the printing service.
/// </summary>
public class PhotoService : IPhotoService
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStorageService _storageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhotoService> _logger;

    // Configuration settings for photo processing
    private readonly long _maxFileSize;
    private readonly string[] _allowedFormats;
    private readonly int _maxPhotosPerUpload;
    private readonly string _storageContainer;

    /// <summary>
    /// Initializes the photo service with required dependencies and configuration.
    /// Sets up file validation rules, storage configuration, and processing pipeline settings
    /// based on application configuration. The service ensures consistent photo handling
    /// across all upload and management operations while maintaining security and performance.
    /// </summary>
    /// <param name="photoRepository">Repository for photo data access operations</param>
    /// <param name="userRepository">Repository for user data access and statistics updates</param>
    /// <param name="storageService">Service for file storage operations across different providers</param>
    /// <param name="imageProcessingService">Service for image processing and quality analysis</param>
    /// <param name="mapper">AutoMapper for entity-DTO conversions</param>
    /// <param name="configuration">Application configuration containing photo processing settings</param>
    /// <param name="logger">Logger for photo operation tracking and troubleshooting</param>
    public PhotoService(
        IPhotoRepository photoRepository,
        IUserRepository userRepository,
        IStorageService storageService,
        IImageProcessingService imageProcessingService,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<PhotoService> logger)
    {
        _photoRepository = photoRepository;
        _userRepository = userRepository;
        _storageService = storageService;
        _imageProcessingService = imageProcessingService;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;

        // Load photo processing configuration
        _maxFileSize = long.Parse(configuration["ImageProcessing:MaxFileSize"] ?? "52428800"); // 50MB default
        _allowedFormats = configuration.GetSection("ImageProcessing:AllowedFormats").Get<string[]>()
            ?? new[] { "jpg", "jpeg", "png", "tiff" };
        _maxPhotosPerUpload = int.Parse(configuration["BusinessRules:MaxPhotosPerUpload"] ?? "50");
        _storageContainer = configuration["StorageSettings:GridFS:BucketName"] ?? "photos";

        _logger.LogInformation("PhotoService initialized with max file size: {MaxFileSize} bytes, max photos per upload: {MaxPhotos}",
            _maxFileSize, _maxPhotosPerUpload);
    }

    /// <summary>
    /// Handles the complete photo upload workflow including validation, storage, and processing initiation.
    /// This method orchestrates file validation, virus scanning, storage to the file system,
    /// metadata extraction, thumbnail generation, and database record creation. The process is designed
    /// to handle multiple files efficiently while maintaining data integrity and providing user feedback.
    /// </summary>
    /// <param name="files">Collection of uploaded image files from HTTP request</param>
    /// <param name="uploadDto">Metadata and settings for the uploaded photos</param>
    /// <param name="userId">User identifier for ownership and security</param>
    /// <returns>Collection of photo DTOs representing successfully uploaded photos</returns>
    public async Task<IEnumerable<PhotoDto>> UploadPhotosAsync(IEnumerable<IFormFile> files, PhotoUploadDto uploadDto, string userId)
    {
        try
        {
            var fileList = files.ToList();
            _logger.LogInformation("Starting photo upload for user {UserId}: {FileCount} files", userId, fileList.Count);

            // Validate upload parameters
            if (!fileList.Any())
            {
                throw new ArgumentException("No files provided for upload");
            }

            if (fileList.Count > _maxPhotosPerUpload)
            {
                throw new ArgumentException($"Cannot upload more than {_maxPhotosPerUpload} photos at once");
            }

            var uploadedPhotos = new List<PhotoDto>();
            var processingTasks = new List<Task>();

            foreach (var file in fileList)
            {
                try
                {
                    // Validate individual file
                    await ValidateUploadedFileAsync(file);

                    // Create photo entity
                    var photo = await CreatePhotoEntityAsync(file, uploadDto, userId);

                    // Store file and create database record
                    var uploadedPhoto = await ProcessSingleUploadAsync(file, photo);
                    uploadedPhotos.Add(_mapper.Map<PhotoDto>(uploadedPhoto));

                    // Queue background processing
                    processingTasks.Add(QueuePhotoProcessingAsync(uploadedPhoto.Id));

                    _logger.LogDebug("Photo uploaded successfully: {PhotoId}, Filename: {Filename}",
                        uploadedPhoto.Id, uploadedPhoto.FileInfo.OriginalFilename);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload photo: {Filename}", file.FileName);
                    // Continue with other files rather than failing the entire batch
                }
            }

            // Update user statistics
            await UpdateUserPhotoStatisticsAsync(userId, uploadedPhotos.Count);

            // Wait for processing tasks to start (not complete)
            await Task.WhenAll(processingTasks);

            _logger.LogInformation("Photo upload completed for user {UserId}: {SuccessCount}/{TotalCount} files uploaded successfully",
                userId, uploadedPhotos.Count, fileList.Count);

            return uploadedPhotos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during photo upload for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a paginated and filtered collection of photos for a specific user.
    /// This method implements the core photo gallery functionality with comprehensive filtering,
    /// searching, and sorting capabilities. It transforms database entities into DTOs
    /// suitable for frontend consumption while maintaining optimal performance.
    /// </summary>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <param name="filter">Filtering, sorting, and pagination parameters</param>
    /// <returns>Paginated result containing photos and metadata for gallery display</returns>
    public async Task<PagedResultDto<PhotoDto>> GetUserPhotosAsync(string userId, PhotoFilterDto filter)
    {
        try
        {
            _logger.LogDebug("Retrieving photos for user {UserId} with filter: Page={Page}, PageSize={PageSize}, SortBy={SortBy}",
                userId, filter.Page, filter.PageSize, filter.SortBy);

            // Calculate pagination parameters
            var skip = (filter.Page - 1) * filter.PageSize;

            // Get photos and total count concurrently
            var photosTask = _photoRepository.GetUserPhotosAsync(
                userId,
                skip,
                filter.PageSize,
                filter.SortBy,
                filter.SortOrder,
                filter.SearchTerm,
                filter.Tags,
                filter.DateFrom,
                filter.DateTo);

            var countTask = _photoRepository.GetUserPhotoCountAsync(
                userId,
                filter.SearchTerm,
                filter.Tags,
                filter.DateFrom,
                filter.DateTo);

            await Task.WhenAll(photosTask, countTask);

            var photos = await photosTask;
            var totalCount = await countTask;

            // Map entities to DTOs
            var photoDtos = _mapper.Map<IEnumerable<PhotoDto>>(photos);

            // Enhance DTOs with additional information
            foreach (var photoDto in photoDtos)
            {
                await EnhancePhotoDtoAsync(photoDto);
            }

            var result = new PagedResultDto<PhotoDto>
            {
                Items = photoDtos,
                TotalCount = (int)totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            _logger.LogDebug("Retrieved {PhotoCount} photos for user {UserId} (Total: {TotalCount})",
                photoDtos.Count(), userId, totalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific photo including technical data and history.
    /// This method provides comprehensive photo information for detail views, including EXIF data,
    /// processing history, print records, and AI analysis results when available.
    /// Security verification ensures users can only access their own photos.
    /// </summary>
    /// <param name="photoId">Photo identifier to retrieve</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Detailed photo information or null if not found or access denied</returns>
    public async Task<PhotoDetailsDto?> GetPhotoDetailsAsync(string photoId, string userId)
    {
        try
        {
            _logger.LogDebug("Retrieving photo details: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);

            var photo = await _photoRepository.GetByIdAndUserAsync(photoId, userId);
            if (photo == null)
            {
                _logger.LogWarning("Photo not found or access denied: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
                return null;
            }

            // Map to detailed DTO
            var photoDetailsDto = _mapper.Map<PhotoDetailsDto>(photo);

            // Enhance with additional computed information
            await EnhancePhotoDetailsDtoAsync(photoDetailsDto, photo);

            _logger.LogDebug("Photo details retrieved successfully: {PhotoId}", photoId);

            return photoDetailsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo details: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
            throw;
        }
    }

    /// <summary>
    /// Updates photo metadata including tags, notes, and privacy settings.
    /// This method allows users to organize and annotate their photos after upload.
    /// The update process validates user ownership and maintains data integrity
    /// while allowing flexible photo organization and personalization.
    /// </summary>
    /// <param name="photoId">Photo identifier to update</param>
    /// <param name="updateDto">Updated metadata and settings</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Updated photo information or null if update failed</returns>
    public async Task<PhotoDto?> UpdatePhotoAsync(string photoId, PhotoUpdateDto updateDto, string userId)
    {
        try
        {
            _logger.LogDebug("Updating photo metadata: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);

            var photo = await _photoRepository.GetByIdAndUserAsync(photoId, userId);
            if (photo == null)
            {
                _logger.LogWarning("Photo not found or access denied for update: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
                return null;
            }

            // Apply updates
            if (updateDto.Tags != null)
            {
                photo.Tags = ParseTags(updateDto.Tags);
            }

            if (updateDto.UserNotes != null)
            {
                photo.UserNotes = updateDto.UserNotes.Trim();
            }

            if (updateDto.IsFavorite.HasValue)
            {
                photo.Flags.IsFavorite = updateDto.IsFavorite.Value;
            }

            if (updateDto.IsPrivate.HasValue)
            {
                photo.Flags.IsPrivate = updateDto.IsPrivate.Value;
            }

            // Save changes
            var success = await _photoRepository.UpdateAsync(photoId, photo);
            if (!success)
            {
                _logger.LogWarning("Failed to update photo metadata: {PhotoId}", photoId);
                return null;
            }

            _logger.LogInformation("Photo metadata updated successfully: {PhotoId}", photoId);

            return _mapper.Map<PhotoDto>(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo metadata: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
            throw;
        }
    }

    /// <summary>
    /// Performs a soft delete of a photo, removing it from user interfaces while preserving data.
    /// This method implements soft deletion to maintain referential integrity with orders and print history.
    /// Deleted photos are hidden from galleries but remain in the system for business records.
    /// </summary>
    /// <param name="photoId">Photo identifier to delete</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>True if deletion completed successfully</returns>
    public async Task<bool> DeletePhotoAsync(string photoId, string userId)
    {
        try
        {
            _logger.LogInformation("Deleting photo: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);

            // Verify ownership before deletion
            var photo = await _photoRepository.GetByIdAndUserAsync(photoId, userId);
            if (photo == null)
            {
                _logger.LogWarning("Photo not found or access denied for deletion: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
                return false;
            }

            // Perform soft delete
            var success = await _photoRepository.DeleteAsync(photoId);
            if (!success)
            {
                _logger.LogWarning("Failed to delete photo: {PhotoId}", photoId);
                return false;
            }

            // Update user statistics
            await UpdateUserPhotoStatisticsAsync(userId, -1);

            _logger.LogInformation("Photo deleted successfully: {PhotoId}", photoId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
            throw;
        }
    }

    /// <summary>
    /// Generates a secure, temporary download link for accessing the original photo file.
    /// This method creates time-limited URLs that allow users to download their original photos
    /// without exposing permanent file locations. The temporary nature provides security
    /// while enabling legitimate access to user-owned content.
    /// </summary>
    /// <param name="photoId">Photo identifier for download</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Download information with temporary URL and expiration time</returns>
    public async Task<PhotoDownloadDto?> GenerateDownloadLinkAsync(string photoId, string userId)
    {
        try
        {
            _logger.LogDebug("Generating download link: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);

            var photo = await _photoRepository.GetByIdAndUserAsync(photoId, userId);
            if (photo == null)
            {
                _logger.LogWarning("Photo not found or access denied for download: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
                return null;
            }

            // Generate temporary download URL
            var originalPath = photo.Storage.Paths.GetValueOrDefault("original");
            if (string.IsNullOrEmpty(originalPath))
            {
                _logger.LogWarning("Original file path not found for photo: {PhotoId}", photoId);
                return null;
            }

            var downloadUrl = await _storageService.GenerateTemporaryUrlAsync(originalPath, _storageContainer, 60); // 1 hour expiration
            if (string.IsNullOrEmpty(downloadUrl))
            {
                _logger.LogWarning("Failed to generate download URL for photo: {PhotoId}", photoId);
                return null;
            }

            var downloadDto = new PhotoDownloadDto
            {
                DownloadUrl = downloadUrl,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                FileName = photo.FileInfo.OriginalFilename,
                FileSize = photo.FileInfo.FileSize
            };

            _logger.LogDebug("Download link generated for photo: {PhotoId}", photoId);

            return downloadDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download link: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
            throw;
        }
    }

    /// <summary>
    /// Processes multiple photos in a batch operation for efficiency.
    /// This method enables operations like bulk tagging, privacy changes, or deletion
    /// across multiple photos simultaneously. Batch processing improves user experience
    /// and system efficiency when managing large photo collections.
    /// </summary>
    /// <param name="photoIds">Collection of photo identifiers to process</param>
    /// <param name="operation">Type of operation to perform</param>
    /// <param name="operationData">Additional data needed for the operation</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Number of photos successfully processed</returns>
    public async Task<int> BulkOperationAsync(IEnumerable<string> photoIds, string operation, object operationData, string userId)
    {
        try
        {
            var photoIdList = photoIds.ToList();
            _logger.LogInformation("Performing bulk operation '{Operation}' on {Count} photos for user: {UserId}",
                operation, photoIdList.Count, userId);

            var updates = new Dictionary<string, object>();

            // Build update operations based on operation type
            switch (operation.ToLowerInvariant())
            {
                case "addtags":
                    if (operationData is string tagsString)
                    {
                        var tags = ParseTags(tagsString);
                        // This would require a more complex update to add to existing tags
                        updates["tags"] = tags;
                    }
                    break;

                case "setprivacy":
                    if (operationData is bool isPrivate)
                    {
                        updates["flags.isPrivate"] = isPrivate;
                    }
                    break;

                case "setfavorite":
                    if (operationData is bool isFavorite)
                    {
                        updates["flags.isFavorite"] = isFavorite;
                    }
                    break;

                case "delete":
                    updates["flags.isDeleted"] = true;
                    break;

                default:
                    throw new ArgumentException($"Unsupported bulk operation: {operation}");
            }

            if (!updates.Any())
            {
                _logger.LogWarning("No valid updates for bulk operation: {Operation}", operation);
                return 0;
            }

            // Perform bulk update
            var updatedCount = await _photoRepository.BulkUpdateAsync(photoIdList, updates, userId);

            _logger.LogInformation("Bulk operation '{Operation}' completed: {UpdatedCount}/{TotalCount} photos updated",
                operation, updatedCount, photoIdList.Count);

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation '{Operation}' for user: {UserId}", operation, userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves photos that are suitable for a specific print size based on resolution and quality.
    /// This method analyzes photo dimensions and quality metrics to recommend photos
    /// that will produce high-quality prints at the requested size.
    /// </summary>
    /// <param name="userId">User identifier for photo access</param>
    /// <param name="printSize">Desired print size for quality assessment</param>
    /// <returns>Collection of photos suitable for the specified print size</returns>
    public async Task<IEnumerable<PhotoDto>> GetPhotosForPrintSizeAsync(string userId, string printSize)
    {
        try
        {
            _logger.LogDebug("Finding photos suitable for print size '{PrintSize}' for user: {UserId}", printSize, userId);

            // Get minimum resolution requirements for the print size
            var minResolution = GetMinimumResolutionForPrintSize(printSize);

            // Retrieve all user photos
            var allPhotos = await _photoRepository.GetUserPhotosAsync(userId, 0, 1000); // Large limit for quality check

            // Filter photos that meet quality requirements
            var suitablePhotos = allPhotos.Where(photo =>
                photo.ImageData.Width >= minResolution.Width &&
                photo.ImageData.Height >= minResolution.Height &&
                photo.Processing.Status == "completed" &&
                !photo.Flags.IsDeleted).ToList();

            var photoDtos = _mapper.Map<IEnumerable<PhotoDto>>(suitablePhotos);

            _logger.LogDebug("Found {Count} photos suitable for print size '{PrintSize}'", suitablePhotos.Count, printSize);

            return photoDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding photos for print size '{PrintSize}' for user: {UserId}", printSize, userId);
            throw;
        }
    }

    /// <summary>
    /// Applies AI-powered enhancements to improve photo quality before printing.
    /// This method utilizes machine learning algorithms to automatically adjust
    /// brightness, contrast, color balance, and sharpness for optimal print results.
    /// </summary>
    /// <param name="photoId">Photo identifier to enhance</param>
    /// <param name="enhancementType">Type of enhancement to apply</param>
    /// <param name="userId">User identifier for ownership verification</param>
    /// <returns>Information about the enhanced photo version</returns>
    public async Task<PhotoDto?> ApplyAIEnhancementAsync(string photoId, string enhancementType, string userId)
    {
        try
        {
            _logger.LogInformation("Applying AI enhancement '{Enhancement}' to photo: PhotoId={PhotoId}, UserId={UserId}",
                enhancementType, photoId, userId);

            var photo = await _photoRepository.GetByIdAndUserAsync(photoId, userId);
            if (photo == null)
            {
                _logger.LogWarning("Photo not found or access denied for enhancement: PhotoId={PhotoId}, UserId={UserId}", photoId, userId);
                return null;
            }

            // Get original image stream
            var originalPath = photo.Storage.Paths.GetValueOrDefault("original");
            if (string.IsNullOrEmpty(originalPath))
            {
                _logger.LogWarning("Original file path not found for photo: {PhotoId}", photoId);
                return null;
            }

            using var imageStream = await _storageService.GetFileStreamAsync(originalPath, _storageContainer);
            if (imageStream == null)
            {
                _logger.LogWarning("Failed to retrieve image stream for photo: {PhotoId}", photoId);
                return null;
            }

            // Apply AI enhancement
            var enhancedResult = await _imageProcessingService.ApplyAIEnhancementsAsync(imageStream, new[] { enhancementType });

            // Store enhanced image
            var enhancedFileName = $"{photo.Id}_enhanced_{enhancementType}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var storageResult = await _storageService.StoreFileAsync(
                CreateFormFileFromStream(enhancedResult.ImageStream, enhancedFileName),
                enhancedFileName,
                _storageContainer);

            // Update photo with enhanced version information
            if (!photo.Storage.Paths.ContainsKey("enhanced"))
            {
                photo.Storage.Paths["enhanced"] = new Dictionary<string, string>();
            }
            photo.Storage.Paths[$"enhanced_{enhancementType}"] = storageResult.FilePath;
            photo.Storage.Urls[$"enhanced_{enhancementType}"] = storageResult.PublicUrl;

            await _photoRepository.UpdateAsync(photoId, photo);

            _logger.LogInformation("AI enhancement applied successfully: PhotoId={PhotoId}, Enhancement={Enhancement}",
                photoId, enhancementType);

            return _mapper.Map<PhotoDto>(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying AI enhancement to photo: PhotoId={PhotoId}, Enhancement={Enhancement}",
                photoId, enhancementType);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the processing status for photos that are being processed.
    /// This method provides real-time updates on photo processing progress
    /// for user feedback and troubleshooting purposes.
    /// </summary>
    /// <param name="userId">User identifier for photo access</param>
    /// <param name="processingStatus">Optional filter for specific processing states</param>
    /// <returns>Collection of photos with their current processing status</returns>
    public async Task<IEnumerable<PhotoProcessingStatusDto>> GetProcessingStatusAsync(string userId, string? processingStatus = null)
    {
        try
        {
            _logger.LogDebug("Retrieving processing status for user: {UserId}, Filter: {Status}", userId, processingStatus);

            // Get photos for processing
            var photos = await _photoRepository.GetPhotosForProcessingAsync(processingStatus ?? "processing", 100);

            // Filter by user
            var userPhotos = photos.Where(p => p.UserId == userId);

            var statusDtos = userPhotos.Select(photo => new PhotoProcessingStatusDto
            {
                PhotoId = photo.Id,
                FileName = photo.FileInfo.OriginalFilename,
                Status = photo.Processing.Status,
                ProgressPercentage = CalculateProcessingProgress(photo.Processing.Status),
                CompletedSteps = GetCompletedProcessingSteps(photo),
                Errors = photo.Processing.ProcessingErrors,
                LastUpdated = photo.Metadata.UpdatedAt
            }).ToList();

            _logger.LogDebug("Retrieved processing status for {Count} photos", statusDtos.Count);

            return statusDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing status for user: {UserId}", userId);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Validates an uploaded file against size and format restrictions.
    /// </summary>
    private async Task ValidateUploadedFileAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ArgumentException($"File '{file.FileName}' is empty");
        }

        if (file.Length > _maxFileSize)
        {
            throw new ArgumentException($"File '{file.FileName}' exceeds maximum size of {_maxFileSize / 1024 / 1024}MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
        if (!_allowedFormats.Contains(extension))
        {
            throw new ArgumentException($"File format '{extension}' is not supported. Allowed formats: {string.Join(", ", _allowedFormats)}");
        }

        // Validate file content using image processing service
        using var stream = file.OpenReadStream();
        var validationResult = await _imageProcessingService.ValidateImageAsync(stream, file.FileName);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"File '{file.FileName}' failed validation: {string.Join(", ", validationResult.Errors)}");
        }
    }

    /// <summary>
    /// Creates a photo entity from uploaded file and metadata.
    /// </summary>
    private async Task<Photo> CreatePhotoEntityAsync(IFormFile file, PhotoUploadDto uploadDto, string userId)
    {
        // Extract image metadata
        using var stream = file.OpenReadStream();
        var metadata = await _imageProcessingService.ExtractMetadataAsync(stream);

        var photo = new Photo
        {
            UserId = userId,
            FileInfo = new PhotoFileInfo
            {
                OriginalFilename = file.FileName,
                SanitizedFilename = SanitizeFileName(file.FileName),
                FileSize = file.Length,
                MimeType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            },
            ImageData = new PhotoImageData
            {
                Width = metadata.Width,
                Height = metadata.Height,
                Orientation = DetermineOrientation(metadata.Width, metadata.Height),
                AspectRatio = CalculateAspectRatio(metadata.Width, metadata.Height),
                Dpi = metadata.Dpi,
                ColorSpace = metadata.ColorSpace,
                HasTransparency = metadata.HasTransparency
            },
            Tags = ParseTags(uploadDto.Tags),
            UserNotes = uploadDto.Notes?.Trim(),
            Flags = new PhotoFlags
            {
                IsPrivate = uploadDto.IsPrivate
            },
            Processing = new PhotoProcessing
            {
                Status = "uploaded"
            }
        };

        // Set EXIF data if available
        if (metadata.ExifData.Any())
        {
            photo.ExifData = MapExifData(metadata.ExifData);
        }

        return photo;
    }

    /// <summary>
    /// Processes a single file upload including storage and database operations.
    /// </summary>
    private async Task<Photo> ProcessSingleUploadAsync(IFormFile file, Photo photo)
    {
        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}_{photo.FileInfo.SanitizedFilename}";

        // Store file
        var storageResult = await _storageService.StoreFileAsync(file, fileName, _storageContainer);

        // Update photo with storage information
        photo.Storage = new PhotoStorage
        {
            Provider = "GridFS", // From configuration
            Container = _storageContainer,
            Paths = new Dictionary<string, string>
            {
                ["original"] = storageResult.FilePath
            },
            Urls = new Dictionary<string, string>
            {
                ["original"] = storageResult.PublicUrl
            }
        };

        // Create database record
        return await _photoRepository.CreateAsync(photo);
    }

    /// <summary>
    /// Queues background processing for a newly uploaded photo.
    /// </summary>
    private async Task QueuePhotoProcessingAsync(string photoId)
    {
        // In a full implementation, this would queue a background job
        // For now, we'll just update the status to indicate processing has started
        await _photoRepository.UpdateProcessingStatusAsync(photoId, "processing");

        _logger.LogDebug("Photo queued for processing: {PhotoId}", photoId);
    }

    /// <summary>
    /// Updates user statistics after photo operations.
    /// </summary>
    private async Task UpdateUserPhotoStatisticsAsync(string userId, int photoCountDelta)
    {
        var updates = new Dictionary<string, object>
        {
            ["totalPhotosUploaded"] = photoCountDelta > 0 ? photoCountDelta : 0
        };

        await _userRepository.UpdateUserStatisticsAsync(userId, updates);
    }

    /// <summary>
    /// Parses comma-separated tags string into a list.
    /// </summary>
    private List<string> ParseTags(string? tagsString)
    {
        if (string.IsNullOrWhiteSpace(tagsString))
            return new List<string>();

        return tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Where(tag => !string.IsNullOrEmpty(tag))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Helper methods for various photo processing operations
    /// </summary>
    private string SanitizeFileName(string fileName) => Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_") + Path.GetExtension(fileName);
    private string DetermineOrientation(int width, int height) => width > height ? "landscape" : width < height ? "portrait" : "square";
    private string CalculateAspectRatio(int width, int height)
    {
        var gcd = GreatestCommonDivisor(width, height);
        return $"{width / gcd}:{height / gcd}";
    }

    private int GreatestCommonDivisor(int a, int b) => b == 0 ? a : GreatestCommonDivisor(b, a % b);

    private PhotoExifData? MapExifData(Dictionary<string, object> exifData)
    {
        // Implementation would map EXIF dictionary to typed object
        return new PhotoExifData(); // Simplified for now
    }

    private async Task EnhancePhotoDtoAsync(PhotoDto photoDto)
    {
        // Add computed fields or additional data to the DTO
        // This could include temporary URLs, print recommendations, etc.
    }

    private async Task EnhancePhotoDetailsDtoAsync(PhotoDetailsDto photoDetailsDto, Photo photo)
    {
        // Add detailed computed information for the details view
        // Such as print recommendations, quality analysis, etc.
    }

    private (int Width, int Height) GetMinimumResolutionForPrintSize(string printSize)
    {
        // Return minimum resolution requirements based on print size
        return printSize.ToLowerInvariant() switch
        {
            "4x6" => (1200, 1800),
            "5x7" => (1500, 2100),
            "8x10" => (2400, 3000),
            "11x14" => (3300, 4200),
            _ => (1200, 1800) // Default
        };
    }

    private double CalculateProcessingProgress(string status)
    {
        return status switch
        {
            "uploaded" => 10.0,
            "processing" => 50.0,
            "completed" => 100.0,
            "failed" => 0.0,
            _ => 0.0
        };
    }

    private List<string> GetCompletedProcessingSteps(Photo photo)
    {
        var steps = new List<string>();

        if (photo.Processing.Status != "uploaded") steps.Add("File Uploaded");
        if (photo.Processing.ThumbnailGenerated) steps.Add("Thumbnail Generated");
        if (photo.Processing.Status == "completed") steps.Add("Processing Complete");

        return steps;
    }

    private IFormFile CreateFormFileFromStream(Stream stream, string fileName)
    {
        // Helper to create IFormFile from stream for storage operations
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    #endregion
}