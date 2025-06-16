using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using MyImage.Application.Interfaces;
using MyImage.Infrastructure.Data.MongoDb;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// GridFS storage service implementation for MongoDB file storage.
/// This service provides secure file storage using MongoDB's GridFS system,
/// which is ideal for storing and serving large files like photos. It handles
/// file upload, retrieval, deletion, and URL generation while maintaining performance and scalability.
/// </summary>
public class GridFSStorageService : IStorageService
{
    private readonly IGridFSBucket _gridFSBucket;
    private readonly ILogger<GridFSStorageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes the GridFS storage service with MongoDB connection and configuration.
    /// Sets up the GridFS bucket for file operations and configures URL generation
    /// for accessing stored files through the API endpoints.
    /// </summary>
    /// <param name="mongoContext">MongoDB context providing database access</param>
    /// <param name="configuration">Application configuration for storage settings</param>
    /// <param name="logger">Logger for storage operation tracking</param>
    public GridFSStorageService(MongoDbContext mongoContext, IConfiguration configuration, ILogger<GridFSStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";

        // Initialize GridFS bucket for photo storage
        var bucketName = configuration["StorageSettings:GridFS:BucketName"] ?? "photos";
        var gridFSOptions = new GridFSBucketOptions
        {
            BucketName = bucketName,
            ChunkSizeBytes = configuration.GetValue<int>("StorageSettings:GridFS:ChunkSizeBytes", 1048576), // 1MB chunks
            WriteConcern = WriteConcern.Acknowledged,
            ReadConcern = ReadConcern.Local
        };

        _gridFSBucket = new GridFSBucket(mongoContext.Database, gridFSOptions);

        _logger.LogInformation("GridFS storage service initialized with bucket: {BucketName}", bucketName);
    }

    /// <summary>
    /// Stores a file in GridFS and returns access information.
    /// This method handles the complete file storage process including metadata preservation,
    /// unique filename generation, and access URL creation for efficient file retrieval.
    /// </summary>
    /// <param name="file">File content to store</param>
    /// <param name="fileName">Desired filename for storage</param>
    /// <param name="container">Storage container (bucket) name</param>
    /// <returns>Storage result with file paths and access URLs</returns>
    public async Task<FileStorageResult> StoreFileAsync(IFormFile file, string fileName, string container)
    {
        try
        {
            _logger.LogDebug("Storing file: {FileName}, Size: {FileSize} bytes", fileName, file.Length);

            // Create unique filename to prevent conflicts
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

            // Prepare GridFS upload options with metadata
            var uploadOptions = new GridFSUploadOptions
            {
                Metadata = new MongoDB.Bson.BsonDocument
                {
                    ["originalName"] = fileName,
                    ["contentType"] = file.ContentType,
                    ["uploadedAt"] = DateTime.UtcNow,
                    ["container"] = container
                }
            };

            // Upload file to GridFS
            using var stream = file.OpenReadStream();
            var objectId = await _gridFSBucket.UploadFromStreamAsync(uniqueFileName, stream, uploadOptions);

            // Generate access URL
            var publicUrl = $"{_baseUrl}/api/files/{objectId}";

            var result = new FileStorageResult
            {
                FilePath = objectId.ToString(),
                PublicUrl = publicUrl,
                Container = container,
                FileSize = file.Length
            };

            _logger.LogInformation("File stored successfully: {FileName} -> {ObjectId}", fileName, objectId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing file: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a file from GridFS as a stream for processing or download.
    /// This method provides efficient access to stored files while maintaining
    /// security boundaries and proper error handling for missing files.
    /// </summary>
    /// <param name="filePath">GridFS ObjectId as string</param>
    /// <param name="container">Storage container name</param>
    /// <returns>File stream for reading stored content</returns>
    public async Task<Stream?> GetFileStreamAsync(string filePath, string container)
    {
        try
        {
            _logger.LogDebug("Retrieving file stream: {FilePath}", filePath);

            if (!MongoDB.Bson.ObjectId.TryParse(filePath, out var objectId))
            {
                _logger.LogWarning("Invalid ObjectId format: {FilePath}", filePath);
                return null;
            }

            // Check if file exists
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, objectId);
            var fileInfo = await _gridFSBucket.Find(filter).FirstOrDefaultAsync();

            if (fileInfo == null)
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }

            // Download file stream
            var stream = await _gridFSBucket.OpenDownloadStreamAsync(objectId);

            _logger.LogDebug("File stream retrieved successfully: {FilePath}", filePath);

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file stream: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Removes a file from GridFS permanently.
    /// This method handles file deletion with proper error handling and logging
    /// while ensuring data consistency and cleanup of GridFS chunks.
    /// </summary>
    /// <param name="filePath">GridFS ObjectId as string</param>
    /// <param name="container">Storage container name</param>
    /// <returns>True if deletion completed successfully</returns>
    public async Task<bool> DeleteFileAsync(string filePath, string container)
    {
        try
        {
            _logger.LogDebug("Deleting file: {FilePath}", filePath);

            if (!MongoDB.Bson.ObjectId.TryParse(filePath, out var objectId))
            {
                _logger.LogWarning("Invalid ObjectId format for deletion: {FilePath}", filePath);
                return false;
            }

            // Delete file from GridFS
            await _gridFSBucket.DeleteAsync(objectId);

            _logger.LogInformation("File deleted successfully: {FilePath}", filePath);

            return true;
        }
        catch (GridFSFileNotFoundException)
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Generates a temporary URL for accessing stored files.
    /// For GridFS implementation, this returns a direct API endpoint URL
    /// since GridFS doesn't support signed URLs like cloud storage providers.
    /// </summary>
    /// <param name="filePath">GridFS ObjectId as string</param>
    /// <param name="container">Storage container name</param>
    /// <param name="expirationMinutes">URL expiration time (not enforced in this implementation)</param>
    /// <returns>Access URL for the file</returns>
    public async Task<string?> GenerateTemporaryUrlAsync(string filePath, string container, int expirationMinutes = 60)
    {
        try
        {
            _logger.LogDebug("Generating temporary URL for file: {FilePath}", filePath);

            if (!MongoDB.Bson.ObjectId.TryParse(filePath, out var objectId))
            {
                _logger.LogWarning("Invalid ObjectId format for URL generation: {FilePath}", filePath);
                return null;
            }

            // Check if file exists
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, objectId);
            var fileInfo = await _gridFSBucket.Find(filter).FirstOrDefaultAsync();

            if (fileInfo == null)
            {
                _logger.LogWarning("File not found for URL generation: {FilePath}", filePath);
                return null;
            }

            // Generate download URL with expiration parameter
            var downloadUrl = $"{_baseUrl}/api/files/{objectId}?expires={DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds()}";

            _logger.LogDebug("Temporary URL generated: {FilePath} -> {Url}", filePath, downloadUrl);

            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating temporary URL for file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Checks if a file exists in GridFS storage.
    /// This method provides efficient existence checking without downloading
    /// the full file content, useful for validation and error handling.
    /// </summary>
    /// <param name="filePath">GridFS ObjectId as string</param>
    /// <param name="container">Storage container name</param>
    /// <returns>True if the file exists</returns>
    public async Task<bool> FileExistsAsync(string filePath, string container)
    {
        try
        {
            _logger.LogDebug("Checking file existence: {FilePath}", filePath);

            if (!MongoDB.Bson.ObjectId.TryParse(filePath, out var objectId))
            {
                return false;
            }

            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, objectId);
            var fileInfo = await _gridFSBucket.Find(filter).FirstOrDefaultAsync();

            var exists = fileInfo != null;

            _logger.LogDebug("File existence check: {FilePath} -> {Exists}", filePath, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return false;
        }
    }
}