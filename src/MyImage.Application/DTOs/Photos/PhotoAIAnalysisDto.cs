using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for AI analysis results.
/// Contains automated analysis and enhancement suggestions for photos.
/// </summary>
public class PhotoAIAnalysisDto
{
    /// <summary>
    /// Detected scene types (e.g., "outdoor", "portrait", "landscape").
    /// </summary>
    public List<string> SceneTypes { get; set; } = new();

    /// <summary>
    /// Dominant colors in the image as hex codes.
    /// </summary>
    public List<string> DominantColors { get; set; } = new();

    /// <summary>
    /// Number of faces detected in the photo.
    /// </summary>
    public int FacesDetected { get; set; }

    /// <summary>
    /// Quality assessment score from 1-10.
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// Specific quality assessments (sharpness, exposure, etc.).
    /// </summary>
    public Dictionary<string, string> QualityAssessments { get; set; } = new();

    /// <summary>
    /// AI-generated suggestions for improvement.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}