using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Photos;

/// <summary>
/// Data Transfer Object for EXIF metadata from photos.
/// Contains photography-specific information extracted from image files.
/// </summary>
public class PhotoExifDto
{
    /// <summary>
    /// Camera make and model information.
    /// </summary>
    public string? Camera { get; set; }

    /// <summary>
    /// Lens information if available.
    /// </summary>
    public string? Lens { get; set; }

    /// <summary>
    /// ISO sensitivity setting.
    /// </summary>
    public int? Iso { get; set; }

    /// <summary>
    /// Aperture setting (f-stop).
    /// </summary>
    public string? Aperture { get; set; }

    /// <summary>
    /// Shutter speed setting.
    /// </summary>
    public string? ShutterSpeed { get; set; }

    /// <summary>
    /// Focal length of the lens.
    /// </summary>
    public string? FocalLength { get; set; }

    /// <summary>
    /// When the photo was actually taken according to camera.
    /// May differ from upload time.
    /// </summary>
    public DateTime? TakenAt { get; set; }
}