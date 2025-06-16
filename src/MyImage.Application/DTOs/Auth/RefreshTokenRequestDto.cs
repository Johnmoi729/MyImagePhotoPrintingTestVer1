using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for refresh token requests.
/// Used when the access token expires but the user session should continue.
/// Implements secure token refresh without requiring user to log in again.
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// The refresh token received during initial authentication.
    /// Must be valid and not expired for the refresh operation to succeed.
    /// Will be invalidated and replaced with a new refresh token after use.
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}