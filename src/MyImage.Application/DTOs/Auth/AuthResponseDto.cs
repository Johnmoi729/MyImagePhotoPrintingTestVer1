using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for successful authentication responses.
/// Contains all information the frontend needs to maintain user session and make authorized requests.
/// This DTO standardizes authentication response format across login and registration endpoints.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// User's unique identifier for API requests and data association.
    /// Frontend stores this to track which user is currently logged in.
    /// Used in subsequent API calls to filter user-specific data.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for display in UI and session management.
    /// Confirms which account is currently authenticated.
    /// Used for logout confirmation and account switching features.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for UI personalization.
    /// Displayed in welcome messages, navigation, and user profile areas.
    /// Creates a personal connection with the authenticated user.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for complete identification.
    /// Used in formal areas of the application and profile management.
    /// Combined with first name for full user identification.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's roles for authorization decisions in the frontend.
    /// Determines which UI elements and features are available to the user.
    /// Frontend uses this to show/hide admin panels, employee tools, etc.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// JWT access token for authenticating API requests.
    /// Must be included in Authorization header for all protected endpoints.
    /// Has shorter expiration time (typically 1 hour) for security.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// Used when access token expires to get a new one without re-login.
    /// Has longer expiration time (typically 30 days) and can be revoked.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in seconds.
    /// Frontend uses this to know when to refresh the token.
    /// Typically set to 3600 seconds (1 hour) for security balance.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// When this authentication response was generated.
    /// Used for client-side session management and debugging.
    /// Helps track authentication flow timing and detect issues.
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}