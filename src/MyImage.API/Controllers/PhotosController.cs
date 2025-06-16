using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using MyImage.Application.Interfaces;

namespace MyImage.API.Controllers;

/// <summary>
/// Photos controller handling all photo management operations in the MyImage system.
/// This controller provides comprehensive endpoints for photo upload, processing, organization, and retrieval.
/// It implements secure file handling, proper HTTP status codes, and efficient pagination for large photo collections
/// while maintaining strict user ownership validation and rate limiting for upload operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("GeneralRequests")]
[ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly ILogger<PhotosController> _logger;
    private readonly IConfiguration _configuration;

    // Configuration for file upload validation
    private readonly long _maxFileSize;
    private readonly int _maxFilesPerRequest;
    private readonly string[] _allowedContentTypes;

    /// <summary>
    /// Initializes the photos controller with required services and configuration.
    /// Sets up dependency injection for photo business logic, logging, and file upload validation rules.
    /// The controller enforces security policies, file size limits, and rate limiting to protect
    /// against abuse while providing a clean REST API interface for photo operations.
    /// </summary>
    /// <param name="photoService">Service handling photo business logic and processing</param>
    /// <param name="logger">Logger for HTTP request tracking and operation monitoring</param>
    /// <param name="configuration">Application configuration for upload limits and validation</param>
    public PhotosController(IPhotoService photoService, ILogger<PhotosController> logger, IConfiguration configuration)
    {
        _photoService = photoService;
        _logger = logger;
        _configuration = configuration;

        // Load file upload configuration
        _maxFileSize = long.Parse(configuration["ImageProcessing:MaxFileSize"] ?? "52428800"); // 50MB
        _maxFilesPerRequest = int.Parse(configuration["BusinessRules:MaxPhotosPerUpload"] ?? "50");
        _allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/tiff" };
    }

    /// <summary>
    /// Retrieves a paginated list of photos for the authenticated user.
    /// This endpoint implements the core photo gallery functionality with comprehensive filtering,
    /// searching, and sorting capabilities. It supports pagination for optimal performance with
    /// large photo collections and provides flexible organization options for users.
    /// </summary>
    /// <param name="filter">Filtering, sorting, and pagination parameters</param>
    /// <returns>Paginated collection of user photos with metadata</returns>
    /// <response code="200">Photos retrieved successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<PhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponseDto<PagedResultDto<PhotoDto>>>> GetPhotos([FromQuery] PhotoFilterDto filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Retrieving photos for user {UserId} with filter: Page={Page}, PageSize={PageSize}",
                userId, filter.Page, filter.PageSize);

            // Validate filter parameters
            if (!ModelState.IsValid)
            {
                var errors = GetModelStateErrors();
                return BadRequest(CreateErrorResponse("Invalid filter parameters", errors));
            }

            // Retrieve photos
            var result = await _photoService.GetUserPhotosAsync(userId, filter);

            _logger.LogDebug("Retrieved {PhotoCount} photos for user {UserId} (Page {Page} of {TotalPages})",
                result.Items.Count(), userId, result.Page, result.TotalPages);

            return Ok(new ApiResponseDto<PagedResultDto<PhotoDto>>
            {
                Success = true,
                Data = result,
                Message = $"Retrieved {result.Items.Count()} photos"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for user: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while retrieving photos"));
        }
    }

    /// <summary>
    /// Retrieves detailed information about a specific photo.
    /// This endpoint provides comprehensive photo information including technical specifications,
    /// EXIF data, processing history, and print records. It enforces user ownership validation
    /// to ensure users can only access their own photos.
    /// </summary>
    /// <param name="id">Photo identifier to retrieve</param>
    /// <returns>Detailed photo information</returns>
    /// <response code="200">Photo details retrieved successfully</response>
    /// <response code="404">Photo not found or access denied</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<PhotoDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<PhotoDetailsDto>>> GetPhoto(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Retrieving photo details: PhotoId={PhotoId}, UserId={UserId}", id, userId);

            var photoDetails = await _photoService.GetPhotoDetailsAsync(id, userId);
            if (photoDetails == null)
            {
                _logger.LogWarning("Photo not found or access denied: PhotoId={PhotoId}, UserId={UserId}", id, userId);
                return NotFound(CreateErrorResponse("Photo not found or access denied"));
            }

            _logger.LogDebug("Photo details retrieved successfully: {PhotoId}", id);

            return Ok(new ApiResponseDto<PhotoDetailsDto>
            {
                Success = true,
                Data = photoDetails,
                Message = "Photo details retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo details: PhotoId={PhotoId}, UserId={UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while retrieving photo details"));
        }
    }

    /// <summary>
    /// Uploads one or more photos for the authenticated user.
    /// This endpoint handles the complete photo upload workflow including file validation,
    /// storage, metadata extraction, and processing initiation. It implements comprehensive
    /// security measures and file validation while supporting batch uploads for efficiency.
    /// </summary>
    /// <param name="files">Photo files to upload</param>
    /// <param name="tags">Optional comma-separated tags for organization</param>
    /// <param name="notes">Optional user notes about the photos</param>
    /// <param name="isPrivate">Whether photos should be marked as private</param>
    /// <returns>Collection of uploaded photo information</returns>
    /// <response code="201">Photos uploaded successfully</response>
    /// <response code="400">Invalid file data or validation errors</response>
    /// <response code="413">Files too large or too many files</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="429">Too many upload requests</response>
    [HttpPost]
    [EnableRateLimiting("FileUpload")]
    [RequestSizeLimit(500_000_000)] // 500MB total request limit
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<PhotoDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PhotoDto>>>> UploadPhotos(
        IFormFileCollection files,
        [FromForm] string? tags = null,
        [FromForm] string? notes = null,
        [FromForm] bool isPrivate = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Photo upload request from user {UserId}: {FileCount} files", userId, files.Count);

            // Validate upload request
            var validationResult = ValidateUploadRequest(files);
            if (!validationResult.IsValid)
            {
                return BadRequest(CreateErrorResponse("Upload validation failed", validationResult.Errors));
            }

            // Create upload DTO
            var uploadDto = new PhotoUploadDto
            {
                Tags = tags,
                Notes = notes,
                IsPrivate = isPrivate,
                FileNames = files.Select(f => f.FileName).ToList()
            };

            // Process upload
            var uploadedPhotos = await _photoService.UploadPhotosAsync(files, uploadDto, userId);

            _logger.LogInformation("Photo upload completed for user {UserId}: {SuccessCount} photos uploaded",
                userId, uploadedPhotos.Count());

            return CreatedAtAction(nameof(GetPhotos), new { }, new ApiResponseDto<IEnumerable<PhotoDto>>
            {
                Success = true,
                Data = uploadedPhotos,
                Message = $"Successfully uploaded {uploadedPhotos.Count()} photos. Processing will begin shortly."
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Photo upload validation failed for user: {UserId}", GetCurrentUserId());
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during photo upload for user: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred during photo upload"));
        }
    }

    /// <summary>
    /// Updates photo metadata including tags, notes, and privacy settings.
    /// This endpoint allows users to organize and annotate their photos after upload.
    /// It validates user ownership and supports partial updates for flexible photo management.
    /// </summary>
    /// <param name="id">Photo identifier to update</param>
    /// <param name="updateDto">Updated metadata and settings</param>
    /// <returns>Updated photo information</returns>
    /// <response code="200">Photo updated successfully</response>
    /// <response code="400">Invalid update data</response>
    /// <response code="404">Photo not found or access denied</response>
    /// <response code="401">User not authenticated</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<PhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<PhotoDto>>> UpdatePhoto(string id, [FromBody] PhotoUpdateDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Updating photo metadata: PhotoId={PhotoId}, UserId={UserId}", id, userId);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = GetModelStateErrors();
                return BadRequest(CreateErrorResponse("Invalid update data", errors));
            }

            // Update photo
            var updatedPhoto = await _photoService.UpdatePhotoAsync(id, updateDto, userId);
            if (updatedPhoto == null)
            {
                _logger.LogWarning("Photo update failed - not found or access denied: PhotoId={PhotoId}, UserId={UserId}", id, userId);
                return NotFound(CreateErrorResponse("Photo not found or access denied"));
            }

            _logger.LogInformation("Photo metadata updated successfully: {PhotoId}", id);

            return Ok(new ApiResponseDto<PhotoDto>
            {
                Success = true,
                Data = updatedPhoto,
                Message = "Photo updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo: PhotoId={PhotoId}, UserId={UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while updating the photo"));
        }
    }

    /// <summary>
    /// Deletes a photo from the user's collection.
    /// This endpoint performs a soft delete to maintain referential integrity with orders
    /// and print history while removing the photo from user interfaces.
    /// </summary>
    /// <param name="id">Photo identifier to delete</param>
    /// <returns>Confirmation of successful deletion</returns>
    /// <response code="200">Photo deleted successfully</response>
    /// <response code="404">Photo not found or access denied</response>
    /// <response code="401">User not authenticated</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<object>>> DeletePhoto(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Deleting photo: PhotoId={PhotoId}, UserId={UserId}", id, userId);

            var success = await _photoService.DeletePhotoAsync(id, userId);
            if (!success)
            {
                _logger.LogWarning("Photo deletion failed - not found or access denied: PhotoId={PhotoId}, UserId={UserId}", id, userId);
                return NotFound(CreateErrorResponse("Photo not found or access denied"));
            }

            _logger.LogInformation("Photo deleted successfully: {PhotoId}", id);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Photo deleted successfully",
                Data = new { Deleted = true, PhotoId = id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: PhotoId={PhotoId}, UserId={UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while deleting the photo"));
        }
    }

    /// <summary>
    /// Generates a temporary download link for accessing the original photo file.
    /// This endpoint creates time-limited URLs for secure file access without exposing
    /// permanent storage locations. It validates user ownership before generating access links.
    /// </summary>
    /// <param name="id">Photo identifier for download</param>
    /// <returns>Download information with temporary URL and expiration</returns>
    /// <response code="200">Download link generated successfully</response>
    /// <response code="404">Photo not found or access denied</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("{id}/download")]
    [ProducesResponseType(typeof(ApiResponseDto<PhotoDownloadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<PhotoDownloadDto>>> GenerateDownloadLink(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Generating download link: PhotoId={PhotoId}, UserId={UserId}", id, userId);

            var downloadInfo = await _photoService.GenerateDownloadLinkAsync(id, userId);
            if (downloadInfo == null)
            {
                _logger.LogWarning("Download link generation failed - photo not found: PhotoId={PhotoId}, UserId={UserId}", id, userId);
                return NotFound(CreateErrorResponse("Photo not found or access denied"));
            }

            _logger.LogDebug("Download link generated successfully: {PhotoId}", id);

            return Ok(new ApiResponseDto<PhotoDownloadDto>
            {
                Success = true,
                Data = downloadInfo,
                Message = "Download link generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download link: PhotoId={PhotoId}, UserId={UserId}", id, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while generating download link"));
        }
    }

    /// <summary>
    /// Performs bulk operations on multiple photos for efficiency.
    /// This endpoint enables operations like bulk tagging, privacy changes, or deletion
    /// across multiple photos simultaneously, improving user experience for large collections.
    /// </summary>
    /// <param name="bulkRequest">Bulk operation request with photo IDs and operation details</param>
    /// <returns>Number of photos successfully processed</returns>
    /// <response code="200">Bulk operation completed successfully</response>
    /// <response code="400">Invalid bulk operation request</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("bulk-actions")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponseDto<object>>> BulkOperation([FromBody] BulkOperationRequestDto bulkRequest)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Bulk operation '{Operation}' requested for {Count} photos by user {UserId}",
                bulkRequest.Action, bulkRequest.PhotoIds.Count(), userId);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = GetModelStateErrors();
                return BadRequest(CreateErrorResponse("Invalid bulk operation request", errors));
            }

            // Validate photo IDs
            if (!bulkRequest.PhotoIds.Any())
            {
                return BadRequest(CreateErrorResponse("No photos selected for bulk operation"));
            }

            // Perform bulk operation
            var updatedCount = await _photoService.BulkOperationAsync(
                bulkRequest.PhotoIds,
                bulkRequest.Action,
                bulkRequest.Data ?? new object(),
                userId);

            _logger.LogInformation("Bulk operation '{Operation}' completed: {UpdatedCount}/{TotalCount} photos processed",
                bulkRequest.Action, updatedCount, bulkRequest.PhotoIds.Count());

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = $"Bulk operation completed: {updatedCount} photos processed",
                Data = new
                {
                    Operation = bulkRequest.Action,
                    ProcessedCount = updatedCount,
                    TotalRequested = bulkRequest.PhotoIds.Count()
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid bulk operation request from user: {UserId}", GetCurrentUserId());
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk operation for user: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred during bulk operation"));
        }
    }

    /// <summary>
    /// Retrieves photos suitable for a specific print size based on quality analysis.
    /// This endpoint analyzes photo resolution and quality metrics to recommend photos
    /// that will produce high-quality prints at the requested size.
    /// </summary>
    /// <param name="printSize">Desired print size for quality assessment</param>
    /// <returns>Collection of photos suitable for the specified print size</returns>
    /// <response code="200">Suitable photos retrieved successfully</response>
    /// <response code="400">Invalid print size</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("for-print-size/{printSize}")]
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<PhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PhotoDto>>>> GetPhotosForPrintSize(string printSize)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Finding photos suitable for print size '{PrintSize}' for user {UserId}", printSize, userId);

            if (string.IsNullOrWhiteSpace(printSize))
            {
                return BadRequest(CreateErrorResponse("Print size is required"));
            }

            var suitablePhotos = await _photoService.GetPhotosForPrintSizeAsync(userId, printSize);

            _logger.LogDebug("Found {Count} photos suitable for print size '{PrintSize}'", suitablePhotos.Count(), printSize);

            return Ok(new ApiResponseDto<IEnumerable<PhotoDto>>
            {
                Success = true,
                Data = suitablePhotos,
                Message = $"Found {suitablePhotos.Count()} photos suitable for {printSize} prints"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding photos for print size '{PrintSize}' for user {UserId}", printSize, GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while finding suitable photos"));
        }
    }

    /// <summary>
    /// Retrieves the processing status for photos that are being processed.
    /// This endpoint provides real-time updates on photo processing progress
    /// including thumbnail generation, quality analysis, and AI enhancement availability.
    /// </summary>
    /// <param name="status">Optional filter for specific processing states</param>
    /// <returns>Collection of photos with their current processing status</returns>
    /// <response code="200">Processing status retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("processing-status")]
    [ProducesResponseType(typeof(ApiResponseDto<IEnumerable<PhotoProcessingStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponseDto<IEnumerable<PhotoProcessingStatusDto>>>> GetProcessingStatus([FromQuery] string? status = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated"));
            }

            _logger.LogDebug("Retrieving processing status for user {UserId}, filter: {Status}", userId, status);

            var processingStatus = await _photoService.GetProcessingStatusAsync(userId, status);

            _logger.LogDebug("Retrieved processing status for {Count} photos", processingStatus.Count());

            return Ok(new ApiResponseDto<IEnumerable<PhotoProcessingStatusDto>>
            {
                Success = true,
                Data = processingStatus,
                Message = $"Retrieved processing status for {processingStatus.Count()} photos"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing status for user {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateErrorResponse("An error occurred while retrieving processing status"));
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims.
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Validates file upload requests against size and format restrictions.
    /// </summary>
    private (bool IsValid, List<ErrorDto> Errors) ValidateUploadRequest(IFormFileCollection files)
    {
        var errors = new List<ErrorDto>();

        // Check if files provided
        if (!files.Any())
        {
            errors.Add(new ErrorDto { Field = "files", Message = "No files provided for upload" });
            return (false, errors);
        }

        // Check file count limit
        if (files.Count > _maxFilesPerRequest)
        {
            errors.Add(new ErrorDto
            {
                Field = "files",
                Message = $"Cannot upload more than {_maxFilesPerRequest} files at once"
            });
        }

        // Validate each file
        foreach (var file in files)
        {
            // Check file size
            if (file.Length > _maxFileSize)
            {
                errors.Add(new ErrorDto
                {
                    Field = "files",
                    Message = $"File '{file.FileName}' exceeds maximum size of {_maxFileSize / 1024 / 1024}MB"
                });
            }

            // Check content type
            if (!_allowedContentTypes.Contains(file.ContentType))
            {
                errors.Add(new ErrorDto
                {
                    Field = "files",
                    Message = $"File '{file.FileName}' has unsupported format. Allowed: JPEG, PNG, TIFF"
                });
            }

            // Check if file is empty
            if (file.Length == 0)
            {
                errors.Add(new ErrorDto
                {
                    Field = "files",
                    Message = $"File '{file.FileName}' is empty"
                });
            }
        }

        return (!errors.Any(), errors);
    }

    /// <summary>
    /// Extracts validation errors from ModelState.
    /// </summary>
    private List<ErrorDto> GetModelStateErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => new ErrorDto { Message = e.ErrorMessage })
            .ToList();
    }

    /// <summary>
    /// Creates a standardized error response.
    /// </summary>
    private ApiResponseDto<object> CreateErrorResponse(string message, List<ErrorDto>? errors = null)
    {
        return new ApiResponseDto<object>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<ErrorDto> { new() { Message = message } }
        };
    }

    #endregion
}

/// <summary>
/// DTO for bulk operation requests on multiple photos.
/// </summary>
public class BulkOperationRequestDto
{
    /// <summary>
    /// Collection of photo IDs to operate on.
    /// </summary>
    public required IEnumerable<string> PhotoIds { get; set; }

    /// <summary>
    /// Type of operation to perform (addTags, setPrivacy, setFavorite, delete).
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Additional data needed for the operation.
    /// </summary>
    public object? Data { get; set; }
}