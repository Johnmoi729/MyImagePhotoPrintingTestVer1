using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo upload requests.
/// This DTO represents the metadata that accompanies file uploads during the photo upload process.
/// The actual file data comes through multipart/form-data, while this DTO contains the descriptive information.
/// This separation allows us to validate metadata independently of file validation and processing.
/// </summary>
public class PhotoUploadDto
{
    /// <summary>
    /// User-defined tags for organizing and categorizing the uploaded photos.
    /// Tags help users find their photos later through search and filtering functionality.
    /// Comma-separated string that will be parsed into individual tags during processing.
    /// Example: "vacation,summer,beach,family" becomes ["vacation", "summer", "beach", "family"]
    /// </summary>
    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters total")]
    public string? Tags { get; set; }

    /// <summary>
    /// User's personal notes or description about the uploaded photos.
    /// This free-form text allows users to add context, memories, or special printing instructions.
    /// Particularly useful for photo collections where users want to remember the story behind the images.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether these photos should be marked as private.
    /// Private photos are only visible to the owner and won't appear in any shared galleries or public features.
    /// Important for sensitive photos that users want to keep completely personal.
    /// </summary>
    public bool IsPrivate { get; set; } = false;

    /// <summary>
    /// Collection of file names being uploaded in this batch.
    /// Used for validation and to ensure all expected files are processed correctly.
    /// Helps track upload progress and identify any files that fail to upload.
    /// </summary>
    public List<string> FileNames { get; set; } = new();
}