using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for forgot password requests.
/// Initiates the password reset process by sending reset email to user.
/// Only requires email to start the secure password reset flow.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email address of the account requesting password reset.
    /// Must match an existing account for reset email to be sent.
    /// For security, no indication is given whether email exists or not.
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; } = string.Empty;
}