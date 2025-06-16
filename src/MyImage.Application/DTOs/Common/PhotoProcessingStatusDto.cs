namespace MyImage.Application.DTOs.Common;

/// <summary>
/// DTO for photo processing status information.
/// </summary>
public class PhotoProcessingStatusDto
{
    public string PhotoId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

// Result objects for service operations
public class FileStorageResult
{
    public string FilePath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class ProcessedImageResult
{
    public Stream ImageStream { get; set; } = Stream.Null;
    public string Format { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
}

public class ImageMetadataResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public int Dpi { get; set; }
    public string ColorSpace { get; set; } = string.Empty;
    public Dictionary<string, object> ExifData { get; set; } = new();
    public bool HasTransparency { get; set; }
}

public class ImageValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool IsSafeForProcessing { get; set; }
}

public class ImageQualityAnalysisResult
{
    public double OverallScore { get; set; }
    public Dictionary<string, double> QualityMetrics { get; set; } = new();
    public List<string> RecommendedPrintSizes { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}