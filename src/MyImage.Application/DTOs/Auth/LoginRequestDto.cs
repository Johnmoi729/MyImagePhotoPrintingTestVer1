using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for user login requests.
/// Contains the credentials needed for authentication and defines validation rules.
/// This DTO protects our API by ensuring only valid login attempts reach our business logic.
/// Maps to the /api/auth/login endpoint request body.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// User's email address for authentication.
    /// Must be a valid email format and is required for all login attempts.
    /// Case-insensitive lookup will be performed in the database.
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password for authentication.
    /// Will be validated against the stored BCrypt hash in the database.
    /// Minimum length enforced for security, but no maximum since it's hashed.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional flag to extend session duration.
    /// When true, refresh token will have longer expiration time.
    /// Implements "Remember Me" functionality for user convenience.
    /// </summary>
    public bool RememberMe { get; set; } = false;
}