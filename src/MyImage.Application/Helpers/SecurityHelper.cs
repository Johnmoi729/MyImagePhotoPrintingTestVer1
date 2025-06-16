namespace MyImage.Application.Helpers;

/// <summary>
/// Utility class for security-related operations and validations.
/// Provides helper methods for password handling, token generation,
/// and other security-sensitive operations throughout the application.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Generates a cryptographically secure random token for verification purposes.
    /// Creates tokens suitable for email verification, password reset, and other
    /// security-sensitive operations with proper entropy and URL-safe encoding.
    /// </summary>
    /// <param name="length">Length of the token in bytes (default 32)</param>
    /// <returns>URL-safe base64 encoded token</returns>
    public static string GenerateSecureToken(int length = 32)
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        // Convert to URL-safe base64
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    /// <summary>
    /// Validates email address format using comprehensive regex pattern.
    /// Provides more thorough email validation than simple attribute validation
    /// for use in business logic and additional security checks.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email format is valid</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Use .NET's built-in email validation
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitizes user input to prevent XSS and injection attacks.
    /// Removes or encodes potentially dangerous characters while preserving
    /// readability for legitimate content like photo notes and tags.
    /// </summary>
    /// <param name="input">User input to sanitize</param>
    /// <returns>Sanitized string safe for storage and display</returns>
    public static string SanitizeUserInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Trim whitespace
        input = input.Trim();

        // Remove or encode potentially dangerous characters
        input = input.Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&#x27;")
                    .Replace("/", "&#x2F;");

        // Remove null bytes and other control characters
        input = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        return input;
    }

    /// <summary>
    /// Generates a secure session identifier for tracking purposes.
    /// Creates unique identifiers for user sessions, request tracking,
    /// and other scenarios requiring secure, unique identification.
    /// </summary>
    /// <returns>Secure session identifier</returns>
    public static string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Validates that a password meets minimum security requirements.
    /// Provides programmatic password validation for use in business logic
    /// beyond attribute validation for comprehensive security checking.
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result with specific error messages</returns>
    public static (bool IsValid, List<string> Errors) ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return (false, errors);
        }

        if (password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number");
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Check for common weak patterns
        if (Regex.IsMatch(password.ToLowerInvariant(), @"(password|123456|qwerty|admin)"))
        {
            errors.Add("Password contains common weak patterns");
        }

        return (!errors.Any(), errors);
    }
}