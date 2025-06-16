using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for reset password requests.
/// Completes the password reset process using token from email.
/// Contains the new password and the verification token from the reset email.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// Password reset token sent to user's email address.
    /// Verifies that the user has access to their email account.
    /// Token expires after a short time (typically 1-24 hours) for security.
    /// </summary>
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password chosen by the user.
    /// Must meet the same complexity requirements as registration.
    /// Will replace the existing password hash in the database.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password to prevent typos.
    /// Must exactly match NewPassword for the reset to proceed.
    /// Helps ensure user can successfully log in with their new password.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Password and confirmation do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}