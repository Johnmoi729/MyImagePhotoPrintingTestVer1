using MyImage.Core.Entities;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Interfaces;

/// <summary>
/// Service interface for file storage operations across different storage providers.
/// This interface abstracts file storage details, allowing our application to work with
/// local storage, cloud storage (Azure Blob, AWS S3), or GridFS without changing business logic.
/// The abstraction enables easy testing, deployment flexibility, and potential provider migration.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Stores a file in the configured storage system and returns access information.
    /// This method handles the actual file storage operation while generating
    /// appropriate file paths, names, and access URLs for the stored content.
    /// Storage location and naming strategies are configured per environment.
    /// </summary>
    /// <param name="file">File content to store</param>
    /// <param name="fileName">Desired filename for the stored file</param>
    /// <param name="container">Storage container or bucket name</param>
    /// <returns>Storage information including paths and access URLs</returns>
    Task<FileStorageResult> StoreFileAsync(IFormFile file, string fileName, string container);

    /// <summary>
    /// Retrieves a file from storage as a stream for processing or download.
    /// This method provides access to stored files for operations like thumbnail generation,
    /// format conversion, or user downloads while maintaining security boundaries
    /// and access control policies.
    /// </summary>
    /// <param name="filePath">Path to the file in the storage system</param>
    /// <param name="container">Storage container or bucket name</param>
    /// <returns>File stream for reading the stored content</returns>
    Task<Stream?> GetFileStreamAsync(string filePath, string container);

    /// <summary>
    /// Removes a file from the storage system permanently.
    /// This method handles file deletion across different storage providers
    /// while ensuring proper cleanup and error handling.
    /// Used during photo deletion or when cleaning up failed uploads.
    /// </summary>
    /// <param name="filePath">Path to the file to delete</param>
    /// <param name="container">Storage container or bucket name</param>
    /// <returns>True if deletion completed successfully</returns>
    Task<bool> DeleteFileAsync(string filePath, string container);

    /// <summary>
    /// Generates a secure, temporary URL for accessing a stored file.
    /// This method creates time-limited access URLs that allow controlled access
    /// to stored files without exposing permanent storage locations.
    /// Temporary URLs provide security while enabling necessary file access.
    /// </summary>
    /// <param name="filePath">Path to the file in storage</param>
    /// <param name="container">Storage container or bucket name</param>
    /// <param name="expirationMinutes">How long the URL should remain valid</param>
    /// <returns>Temporary URL for accessing the file</returns>
    Task<string?> GenerateTemporaryUrlAsync(string filePath, string container, int expirationMinutes = 60);

    /// <summary>
    /// Checks if a file exists in the storage system.
    /// This method provides a way to verify file existence before attempting
    /// operations like retrieval or deletion, enabling better error handling
    /// and user feedback in case of storage issues.
    /// </summary>
    /// <param name="filePath">Path to check for file existence</param>
    /// <param name="container">Storage container or bucket name</param>
    /// <returns>True if the file exists in storage</returns>
    Task<bool> FileExistsAsync(string filePath, string container);
}