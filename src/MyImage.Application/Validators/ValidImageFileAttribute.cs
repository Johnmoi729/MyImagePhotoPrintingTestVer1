using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Validation;

/// <summary>
/// Custom validation attribute for file uploads to ensure proper image file validation.
/// This attribute validates file size, content type, and basic file integrity
/// to prevent malicious uploads and ensure only valid images are processed.
/// </summary>
public class ValidImageFileAttribute : ValidationAttribute
{
    private readonly long _maxFileSize;
    private readonly string[] _allowedContentTypes;

    /// <summary>
    /// Initializes the image file validation attribute with size and type constraints.
    /// </summary>
    /// <param name="maxFileSizeMB">Maximum file size in megabytes</param>
    /// <param name="allowedContentTypes">Array of allowed MIME types</param>
    public ValidImageFileAttribute(int maxFileSizeMB = 50, params string[] allowedContentTypes)
    {
        _maxFileSize = maxFileSizeMB * 1024 * 1024; // Convert MB to bytes
        _allowedContentTypes = allowedContentTypes.Length > 0
            ? allowedContentTypes
            : new[] { "image/jpeg", "image/jpg", "image/png", "image/tiff" };

        ErrorMessage = "Invalid image file";
    }

    /// <summary>
    /// Validates that the uploaded file is a valid image within size and type constraints.
    /// </summary>
    /// <param name="value">The file to validate</param>
    /// <param name="validationContext">Validation context for detailed error reporting</param>
    /// <returns>Validation result indicating success or failure with specific error messages</returns>
    public override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Allow null for optional file uploads
        }

        if (value is IFormFile file)
        {
            return ValidateSingleFile(file, validationContext.MemberName);
        }

        if (value is IFormFileCollection files)
        {
            return ValidateMultipleFiles(files, validationContext.MemberName);
        }

        return new ValidationResult("Value must be an IFormFile or IFormFileCollection");
    }

    /// <summary>
    /// Validates a single uploaded file against all constraints.
    /// </summary>
    private ValidationResult? ValidateSingleFile(IFormFile file, string? memberName)
    {
        // Check file size
        if (file.Length > _maxFileSize)
        {
            return new ValidationResult(
                $"File '{file.FileName}' exceeds maximum size of {_maxFileSize / 1024 / 1024}MB",
                memberName != null ? new[] { memberName } : null);
        }

        // Check content type
        if (!_allowedContentTypes.Contains(file.ContentType))
        {
            return new ValidationResult(
                $"File '{file.FileName}' has unsupported format. Allowed formats: {string.Join(", ", _allowedContentTypes)}",
                memberName != null ? new[] { memberName } : null);
        }

        // Check if file is empty
        if (file.Length == 0)
        {
            return new ValidationResult(
                $"File '{file.FileName}' is empty",
                memberName != null ? new[] { memberName } : null);
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validates multiple uploaded files against all constraints.
    /// </summary>
    private ValidationResult? ValidateMultipleFiles(IFormFileCollection files, string? memberName)
    {
        foreach (var file in files)
        {
            var result = ValidateSingleFile(file, memberName);
            if (result != ValidationResult.Success)
            {
                return result;
            }
        }

        return ValidationResult.Success;
    }
}