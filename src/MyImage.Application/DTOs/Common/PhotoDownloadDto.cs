namespace MyImage.Application.DTOs.Common;

/// <summary>
/// DTO for photo download information with temporary access.
/// </summary>
public class PhotoDownloadDto
{
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}