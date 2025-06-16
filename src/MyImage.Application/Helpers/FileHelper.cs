namespace MyImage.Application.Helpers;

/// <summary>
/// Utility class for common file operations and validations.
/// Provides helper methods for file handling, validation, and processing
/// that are used across multiple parts of the application.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Sanitizes a filename by removing or replacing invalid characters.
    /// Ensures filenames are safe for storage across different file systems
    /// while preserving readability and the original extension.
    /// </summary>
    /// <param name="fileName">Original filename to sanitize</param>
    /// <returns>Sanitized filename safe for storage</returns>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unknown";
        }

        // Get the file extension
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitizedName = new string(nameWithoutExtension
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // Replace spaces with underscores
        sanitizedName = sanitizedName.Replace(' ', '_');

        // Remove consecutive underscores
        sanitizedName = Regex.Replace(sanitizedName, @"_{2,}", "_");

        // Trim underscores from start and end
        sanitizedName = sanitizedName.Trim('_');

        // Ensure name is not empty
        if (string.IsNullOrEmpty(sanitizedName))
        {
            sanitizedName = "file";
        }

        // Limit length
        if (sanitizedName.Length > 100)
        {
            sanitizedName = sanitizedName.Substring(0, 100);
        }

        return sanitizedName + extension.ToLowerInvariant();
    }

    /// <summary>
    /// Validates image file content by checking magic bytes and basic structure.
    /// Provides an additional security layer beyond MIME type checking
    /// by examining the actual file content for image signatures.
    /// </summary>
    /// <param name="stream">File stream to validate</param>
    /// <returns>True if the file appears to be a valid image</returns>
    public static async Task<bool> IsValidImageFileAsync(Stream stream)
    {
        if (stream.Length < 4)
        {
            return false;
        }

        var originalPosition = stream.Position;
        stream.Position = 0;

        try
        {
            var header = new byte[12];
            await stream.ReadAsync(header, 0, header.Length);

            // Check for common image file signatures
            return IsJpegSignature(header) ||
                   IsPngSignature(header) ||
                   IsTiffSignature(header) ||
                   IsGifSignature(header) ||
                   IsWebpSignature(header);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Generates a unique filename to prevent conflicts in storage.
    /// Creates a filename that combines timestamp, random component, and original name
    /// to ensure uniqueness while maintaining some readability.
    /// </summary>
    /// <param name="originalFileName">Original filename</param>
    /// <returns>Unique filename for storage</returns>
    public static string GenerateUniqueFileName(string originalFileName)
    {
        var sanitizedName = SanitizeFileName(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedName);
        var extension = Path.GetExtension(sanitizedName);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var randomComponent = Guid.NewGuid().ToString("N")[..8];

        return $"{timestamp}_{randomComponent}_{nameWithoutExtension}{extension}";
    }

    /// <summary>
    /// Formats file size in human-readable format.
    /// Converts byte counts to appropriate units (B, KB, MB, GB)
    /// for user-friendly display in the interface.
    /// </summary>
    /// <param name="bytes">File size in bytes</param>
    /// <returns>Formatted file size string</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Determines image orientation based on dimensions.
    /// Classifies images as landscape, portrait, or square
    /// for appropriate display and processing decisions.
    /// </summary>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    /// <returns>Orientation string (landscape, portrait, square)</returns>
    public static string DetermineOrientation(int width, int height)
    {
        if (width > height)
            return "landscape";
        else if (height > width)
            return "portrait";
        else
            return "square";
    }

    /// <summary>
    /// Calculates aspect ratio as a simplified fraction string.
    /// Provides user-friendly aspect ratio representation
    /// for display and print size recommendations.
    /// </summary>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <returns>Aspect ratio as string (e.g., "4:3", "16:9")</returns>
    public static string CalculateAspectRatio(int width, int height)
    {
        var gcd = GreatestCommonDivisor(width, height);
        return $"{width / gcd}:{height / gcd}";
    }

    #region Private Helper Methods

    private static bool IsJpegSignature(byte[] header)
    {
        return header.Length >= 3 &&
               header[0] == 0xFF &&
               header[1] == 0xD8 &&
               header[2] == 0xFF;
    }

    private static bool IsPngSignature(byte[] header)
    {
        return header.Length >= 8 &&
               header[0] == 0x89 &&
               header[1] == 0x50 &&
               header[2] == 0x4E &&
               header[3] == 0x47 &&
               header[4] == 0x0D &&
               header[5] == 0x0A &&
               header[6] == 0x1A &&
               header[7] == 0x0A;
    }

    private static bool IsTiffSignature(byte[] header)
    {
        return header.Length >= 4 &&
               ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00) || // Little-endian
                (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A));   // Big-endian
    }

    private static bool IsGifSignature(byte[] header)
    {
        return header.Length >= 6 &&
               header[0] == 0x47 &&
               header[1] == 0x49 &&
               header[2] == 0x46 &&
               header[3] == 0x38 &&
               (header[4] == 0x37 || header[4] == 0x39) &&
               header[5] == 0x61;
    }

    private static bool IsWebpSignature(byte[] header)
    {
        return header.Length >= 12 &&
               header[0] == 0x52 &&
               header[1] == 0x49 &&
               header[2] == 0x46 &&
               header[3] == 0x46 &&
               header[8] == 0x57 &&
               header[9] == 0x45 &&
               header[10] == 0x42 &&
               header[11] == 0x50;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    #endregion
}
