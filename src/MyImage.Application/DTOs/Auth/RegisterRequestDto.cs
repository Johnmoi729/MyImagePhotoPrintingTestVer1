using System.ComponentModel.DataAnnotations;

namespace MyImage.Application.DTOs.Auth;

/// <summary>
/// Data Transfer Object for user registration requests.
/// Contains all information needed to create a new user account with validation rules.
/// This DTO ensures data integrity before user creation and standardizes registration input.
/// Maps to the /api/auth/register endpoint request body.
/// </summary>
public class RegisterRequestDto
{
    /// <summary>
    /// User's email address - must be unique in the system.
    /// Will be used for login, notifications, and account recovery.
    /// Email verification will be sent to this address after registration.
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's chosen password for account security.
    /// Will be hashed using BCrypt before storage - never stored in plain text.
    /// Enforces minimum complexity requirements for account security.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation to prevent typos during registration.
    /// Must exactly match the Password field for registration to proceed.
    /// Client-side validation should check this in real-time for better UX.
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Password and confirmation do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for personalization and communication.
    /// Used in email greetings, order confirmations, and UI personalization.
    /// Required for creating a personal experience with the service.
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for formal communications and order records.
    /// Used in shipping labels, formal correspondence, and account verification.
    /// Required for complete user identification and professional communication.
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number for order notifications and customer support.
    /// Should include country code for international users (+1, +44, etc.).
    /// Used for SMS notifications about order status and delivery updates.
    /// </summary>
    [Phone(ErrorMessage = "Please provide a valid phone number")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Confirmation that user accepts terms of service and privacy policy.
    /// Required by law in many jurisdictions and for business protection.
    /// Must be explicitly checked - pre-checked boxes are not legally binding.
    /// </summary>
    [Required(ErrorMessage = "You must accept the terms and conditions")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
    public bool AcceptTerms { get; set; } = false;

    /// <summary>
    /// Optional marketing consent for promotional emails and offers.
    /// Allows users to opt-in to marketing communications during registration.
    /// Helps with GDPR compliance by explicitly capturing marketing consent.
    /// </summary>
    public bool AcceptMarketing { get; set; } = false;
}