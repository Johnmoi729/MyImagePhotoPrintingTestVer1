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
/// Image processing service implementation using ImageSharp for photo operations.
/// This service handles image validation, metadata extraction, thumbnail generation,
/// and quality analysis for the photo printing workflow. It provides secure image processing
/// with proper format validation and error handling to prevent malicious uploads.
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxImageDimension;
    private readonly int _thumbnailSize;
    private readonly int _previewSize;
    private readonly int _compressionQuality;

    /// <summary>
    /// Initializes the image processing service with configuration and validation settings.
    /// Sets up ImageSharp processing parameters and security constraints based on
    /// application configuration to ensure safe and efficient image handling.
    /// </summary>
    /// <param name="configuration">Application configuration for image processing settings</param>
    /// <param name="logger">Logger for image processing operation tracking</param>
    public ImageProcessingService(IConfiguration configuration, ILogger<ImageProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load image processing configuration
        _maxImageDimension = configuration.GetValue<int>("ImageProcessing:MaxImageDimension", 8000);
        _thumbnailSize = configuration.GetValue<int>("ImageProcessing:ThumbnailSize", 300);
        _previewSize = configuration.GetValue<int>("ImageProcessing:PreviewSize", 800);
        _compressionQuality = configuration.GetValue<int>("ImageProcessing:CompressionQuality", 85);

        _logger.LogInformation("Image processing service initialized with max dimension: {MaxDimension}px", _maxImageDimension);
    }

    /// <summary>
    /// Generates thumbnail images in multiple sizes for gallery display and previews.
    /// This method creates optimized, smaller versions of uploaded photos using high-quality
    /// resampling algorithms while maintaining aspect ratios and visual quality for UI display.
    /// </summary>
    /// <param name="inputStream">Stream containing the original image data</param>
    /// <param name="sizes">Dictionary of size names and pixel dimensions</param>
    /// <returns>Collection of generated thumbnails with storage information</returns>
    public async Task<IEnumerable<ProcessedImageResult>> GenerateThumbnailsAsync(Stream inputStream, Dictionary<string, int> sizes)
    {
        try
        {
            _logger.LogDebug("Generating thumbnails for {Count} sizes", sizes.Count);

            var results = new List<ProcessedImageResult>();

            using var image = await Image.LoadAsync(inputStream);

            foreach (var sizeInfo in sizes)
            {
                var sizeName = sizeInfo.Key;
                var maxDimension = sizeInfo.Value;

                _logger.LogDebug("Generating {SizeName} thumbnail: {MaxDimension}px", sizeName, maxDimension);

                // Create a copy of the image for processing
                using var thumbnail = image.Clone();

                // Calculate new dimensions maintaining aspect ratio
                var (newWidth, newHeight) = CalculateResizedDimensions(image.Width, image.Height, maxDimension);

                // Resize with high-quality resampling
                thumbnail.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

                // Convert to stream
                var outputStream = new MemoryStream();
                await thumbnail.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = _compressionQuality
                });

                outputStream.Position = 0;

                results.Add(new ProcessedImageResult
                {
                    ImageStream = outputStream,
                    Format = "jpeg",
                    Width = newWidth,
                    Height = newHeight,
                    FileSize = outputStream.Length
                });

                _logger.LogDebug("Generated {SizeName} thumbnail: {Width}x{Height}, {FileSize} bytes",
                    sizeName, newWidth, newHeight, outputStream.Length);
            }

            _logger.LogInformation("Generated {Count} thumbnails successfully", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnails");
            throw;
        }
    }

    /// <summary>
    /// Extracts comprehensive metadata from uploaded image files.
    /// This method analyzes image properties, EXIF data, and technical specifications
    /// to provide detailed information for print quality assessment and user display.
    /// </summary>
    /// <param name="inputStream">Stream containing the image to analyze</param>
    /// <returns>Comprehensive metadata including technical details and EXIF information</returns>
    public async Task<ImageMetadataResult> ExtractMetadataAsync(Stream inputStream)
    {
        try
        {
            _logger.LogDebug("Extracting image metadata");

            using var image = await Image.LoadAsync(inputStream);

            var metadata = new ImageMetadataResult
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                ColorSpace = "sRGB", // Default assumption
                HasTransparency = HasTransparency(image)
            };

            // Extract DPI information
            if (image.Metadata.HorizontalResolution > 0)
            {
                metadata.Dpi = (int)Math.Round(image.Metadata.HorizontalResolution);
            }
            else
            {
                metadata.Dpi = 72; // Default web resolution
            }

            // Extract EXIF data if available
            if (image.Metadata.ExifProfile != null)
            {
                metadata.ExifData = ExtractExifData(image.Metadata.ExifProfile);
            }

            _logger.LogDebug("Metadata extracted: {Width}x{Height}, {Format}, {Dpi} DPI",
                metadata.Width, metadata.Height, metadata.Format, metadata.Dpi);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image metadata");
            throw;
        }
    }

    /// <summary>
    /// Validates uploaded images for format support, file integrity, and print suitability.
    /// This method performs comprehensive security and quality checks to ensure uploaded
    /// files are safe for processing and suitable for the photo printing workflow.
    /// </summary>
    /// <param name="inputStream">Stream containing the image to validate</param>
    /// <param name="fileName">Original filename for format detection</param>
    /// <returns>Validation results indicating safety and suitability</returns>
    public async Task<ImageValidationResult> ValidateImageAsync(Stream inputStream, string fileName)
    {
        try
        {
            _logger.LogDebug("Validating image: {FileName}", fileName);

            var result = new ImageValidationResult
            {
                IsValid = true,
                IsSafeForProcessing = true
            };

            // Try to load and validate the image
            try
            {
                using var image = await Image.LoadAsync(inputStream);

                // Check image dimensions
                if (image.Width > _maxImageDimension || image.Height > _maxImageDimension)
                {
                    result.Warnings.Add($"Image dimensions ({image.Width}x{image.Height}) are very large. Consider resizing for better performance.");
                }

                if (image.Width < 100 || image.Height < 100)
                {
                    result.Warnings.Add("Image is very small and may not be suitable for printing.");
                }

                // Check for suspicious image properties
                if (image.Width * image.Height > 100_000_000) // 100 megapixels
                {
                    result.Errors.Add("Image is too large to process safely.");
                    result.IsValid = false;
                    result.IsSafeForProcessing = false;
                }

                _logger.LogDebug("Image validation successful: {Width}x{Height}", image.Width, image.Height);
            }
            catch (InvalidImageContentException ex)
            {
                _logger.LogWarning(ex, "Invalid image content: {FileName}", fileName);
                result.Errors.Add("File is not a valid image or is corrupted.");
                result.IsValid = false;
                result.IsSafeForProcessing = false;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Unsupported image format: {FileName}", fileName);
                result.Errors.Add("Image format is not supported.");
                result.IsValid = false;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image: {FileName}", fileName);

            return new ImageValidationResult
            {
                IsValid = false,
                IsSafeForProcessing = false,
                Errors = { "Unable to validate image file." }
            };
        }
    }

    /// <summary>
    /// Optimizes images for print production (placeholder implementation).
    /// This method would prepare images for high-quality printing by adjusting
    /// resolution, color profiles, and format optimization for specific print types.
    /// </summary>
    public async Task<ProcessedImageResult> OptimizeForPrintAsync(Stream inputStream, string printSize, string printType)
    {
        // Placeholder implementation - in a real system, this would:
        // 1. Adjust DPI for print requirements
        // 2. Convert color space if needed
        // 3. Apply print-specific optimizations

        await Task.Delay(100); // Simulate processing

        using var image = await Image.LoadAsync(inputStream);
        var outputStream = new MemoryStream();

        await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 95 });
        outputStream.Position = 0;

        return new ProcessedImageResult
        {
            ImageStream = outputStream,
            Format = "jpeg",
            Width = image.Width,
            Height = image.Height,
            FileSize = outputStream.Length
        };
    }

    /// <summary>
    /// Applies AI-powered enhancements (placeholder implementation).
    /// This method would use machine learning to improve image quality
    /// through automatic color correction, noise reduction, and sharpening.
    /// </summary>
    public async Task<ProcessedImageResult> ApplyAIEnhancementsAsync(Stream inputStream, IEnumerable<string> enhancementTypes)
    {
        // Placeholder implementation - in a real system, this would:
        // 1. Apply AI-based color correction
        // 2. Reduce noise using ML algorithms
        // 3. Enhance sharpness intelligently

        await Task.Delay(200); // Simulate AI processing

        using var image = await Image.LoadAsync(inputStream);
        var outputStream = new MemoryStream();

        // Apply basic enhancements as placeholder
        image.Mutate(x => x.Contrast(1.1f).Saturate(1.05f));

        await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 90 });
        outputStream.Position = 0;

        return new ProcessedImageResult
        {
            ImageStream = outputStream,
            Format = "jpeg",
            Width = image.Width,
            Height = image.Height,
            FileSize = outputStream.Length
        };
    }

    /// <summary>
    /// Analyzes image quality and provides recommendations (placeholder implementation).
    /// This method would evaluate technical quality metrics and provide
    /// specific recommendations for print sizes and potential improvements.
    /// </summary>
    public async Task<ImageQualityAnalysisResult> AnalyzeQualityAsync(Stream inputStream)
    {
        await Task.Delay(150); // Simulate analysis processing

        using var image = await Image.LoadAsync(inputStream);

        // Calculate basic quality score based on resolution
        var pixelCount = image.Width * image.Height;
        var qualityScore = Math.Min(10.0, (pixelCount / 1_000_000.0) * 2); // Rough quality based on megapixels

        var result = new ImageQualityAnalysisResult
        {
            OverallScore = qualityScore,
            QualityMetrics = new Dictionary<string, double>
            {
                ["resolution"] = qualityScore,
                ["sharpness"] = 7.5, // Placeholder
                ["noise"] = 8.0,     // Placeholder
                ["exposure"] = 7.8   // Placeholder
            }
        };

        // Generate recommendations based on resolution
        if (image.Width >= 3000 && image.Height >= 2400)
        {
            result.RecommendedPrintSizes.AddRange(new[] { "4x6", "5x7", "8x10" });
        }
        else if (image.Width >= 2400 && image.Height >= 1800)
        {
            result.RecommendedPrintSizes.AddRange(new[] { "4x6", "5x7" });
        }
        else if (image.Width >= 1800 && image.Height >= 1200)
        {
            result.RecommendedPrintSizes.Add("4x6");
        }

        if (qualityScore < 6.0)
        {
            result.Suggestions.Add("Consider using AI enhancement to improve image quality");
        }

        return result;
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates new dimensions for resizing while maintaining aspect ratio.
    /// </summary>
    private (int Width, int Height) CalculateResizedDimensions(int originalWidth, int originalHeight, int maxDimension)
    {
        if (originalWidth <= maxDimension && originalHeight <= maxDimension)
        {
            return (originalWidth, originalHeight);
        }

        var aspectRatio = (double)originalWidth / originalHeight;

        if (originalWidth > originalHeight)
        {
            var newWidth = maxDimension;
            var newHeight = (int)(maxDimension / aspectRatio);
            return (newWidth, newHeight);
        }
        else
        {
            var newHeight = maxDimension;
            var newWidth = (int)(maxDimension * aspectRatio);
            return (newWidth, newHeight);
        }
    }

    /// <summary>
    /// Checks if an image has transparency information.
    /// </summary>
    private bool HasTransparency(Image image)
    {
        // Check if image format supports transparency
        return image.Metadata.DecodedImageFormat?.Name?.ToLowerInvariant() switch
        {
            "png" => true,
            "gif" => true,
            "webp" => true,
            _ => false
        };
    }

    /// <summary>
    /// Extracts EXIF data from image metadata.
    /// </summary>
    private Dictionary<string, object> ExtractExifData(ExifProfile exifProfile)
    {
        var exifData = new Dictionary<string, object>();

        try
        {
            // Extract camera information
            if (exifProfile.TryGetValue(ExifTag.Make, out var make))
                exifData["cameraMake"] = make.Value?.ToString() ?? "";

            if (exifProfile.TryGetValue(ExifTag.Model, out var model))
                exifData["cameraModel"] = model.Value?.ToString() ?? "";

            // Extract capture settings
            if (exifProfile.TryGetValue(ExifTag.ISOSpeedRatings, out var iso))
                exifData["iso"] = iso.Value;

            if (exifProfile.TryGetValue(ExifTag.FNumber, out var aperture))
                exifData["aperture"] = aperture.Value;

            if (exifProfile.TryGetValue(ExifTag.ExposureTime, out var exposureTime))
                exifData["shutterSpeed"] = exposureTime.Value;

            if (exifProfile.TryGetValue(ExifTag.DateTime, out var dateTime))
                exifData["captureDate"] = dateTime.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting EXIF data");
        }

        return exifData;
    }

    #endregion
}