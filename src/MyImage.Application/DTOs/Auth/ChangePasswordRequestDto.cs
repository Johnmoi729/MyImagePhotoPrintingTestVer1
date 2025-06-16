using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for change password requests.
/// Allows authenticated users to change their password from their profile.
/// Requires current password for additional security verification.
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// User's current password for verification.
    /// Ensures that only the account owner can change the password.
    /// Protects against unauthorized password changes if session is compromised.
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password chosen by the user.
    /// Must meet complexity requirements and be different from current password.
    /// Will replace the existing password hash after verification.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password.
    /// Must match NewPassword exactly for the change to proceed.
    /// Prevents typos that would lock user out of their account.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Password and confirmation do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}