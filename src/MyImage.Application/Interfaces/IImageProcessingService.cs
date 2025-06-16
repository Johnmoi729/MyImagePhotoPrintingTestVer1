using MyImage.Core.Entities;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Interfaces;

/// <summary>
/// Service interface for image processing operations including resizing, format conversion, and quality optimization.
/// This interface encapsulates all image manipulation logic needed for our photo printing service.
/// Processing operations include thumbnail generation, format standardization, quality assessment, and print optimization.
/// The service handles technical image details while providing a clean business logic interface.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Generates thumbnail images in multiple sizes for gallery display and previews.
    /// This method creates optimized, smaller versions of uploaded photos for efficient loading
    /// in user interfaces. Multiple sizes support different UI contexts like grid thumbnails,
    /// preview images, and detail view images while maintaining visual quality.
    /// </summary>
    /// <param name="inputStream">Stream containing the original image data</param>
    /// <param name="sizes">Dictionary of size names and pixel dimensions for thumbnail generation</param>
    /// <returns>Collection of generated thumbnails with their storage information</returns>
    Task<IEnumerable<ProcessedImageResult>> GenerateThumbnailsAsync(Stream inputStream, Dictionary<string, int> sizes);

    /// <summary>
    /// Extracts comprehensive metadata from uploaded image files.
    /// This method reads EXIF data, analyzes technical specifications, and calculates
    /// quality metrics that help determine appropriate print sizes and settings.
    /// Metadata extraction provides valuable information for both users and print quality optimization.
    /// </summary>
    /// <param name="inputStream">Stream containing the image file to analyze</param>
    /// <returns>Comprehensive metadata including technical details and EXIF information</returns>
    Task<ImageMetadataResult> ExtractMetadataAsync(Stream inputStream);

    /// <summary>
    /// Validates uploaded images for format support, file integrity, and print suitability.
    /// This method performs security checks, format validation, and quality assessment
    /// to ensure uploaded files are safe, supported, and suitable for print processing.
    /// Validation prevents issues downstream in the printing workflow.
    /// </summary>
    /// <param name="inputStream">Stream containing the image file to validate</param>
    /// <param name="fileName">Original filename for format detection</param>
    /// <returns>Validation results indicating whether the image is acceptable for processing</returns>
    Task<ImageValidationResult> ValidateImageAsync(Stream inputStream, string fileName);

    /// <summary>
    /// Optimizes images for print production by adjusting resolution, color profile, and format.
    /// This method prepares images for high-quality printing by ensuring proper DPI,
    /// color space conversion, and format optimization based on the intended print size and type.
    /// Print optimization helps ensure consistent, high-quality results across different print products.
    /// </summary>
    /// <param name="inputStream">Stream containing the original image</param>
    /// <param name="printSize">Target print size for optimization</param>
    /// <param name="printType">Type of print product (standard, premium, canvas, etc.)</param>
    /// <returns>Optimized image ready for print production</returns>
    Task<ProcessedImageResult> OptimizeForPrintAsync(Stream inputStream, string printSize, string printType);

    /// <summary>
    /// Applies AI-powered enhancements to improve image quality automatically.
    /// This method uses machine learning algorithms to detect and correct common image issues
    /// like poor lighting, color balance problems, or sharpness issues.
    /// AI enhancement helps users achieve better print results without manual editing expertise.
    /// </summary>
    /// <param name="inputStream">Stream containing the image to enhance</param>
    /// <param name="enhancementTypes">Types of enhancements to apply</param>
    /// <returns>Enhanced image with improved quality characteristics</returns>
    Task<ProcessedImageResult> ApplyAIEnhancementsAsync(Stream inputStream, IEnumerable<string> enhancementTypes);

    /// <summary>
    /// Analyzes image quality and provides recommendations for print sizes and enhancements.
    /// This method evaluates resolution, sharpness, noise levels, and other quality factors
    /// to recommend appropriate print sizes and suggest improvements.
    /// Quality analysis helps users make informed decisions about their print orders.
    /// </summary>
    /// <param name="inputStream">Stream containing the image to analyze</param>
    /// <returns>Quality analysis results with recommendations and suggestions</returns>
    Task<ImageQualityAnalysisResult> AnalyzeQualityAsync(Stream inputStream);
}