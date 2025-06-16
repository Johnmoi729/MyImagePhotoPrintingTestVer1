using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Validation;

/// <summary>
/// Custom validation attribute for ensuring strong passwords meet security requirements.
/// This attribute enforces password complexity rules including length, character types,
/// and common password patterns to improve account security.
/// </summary>
public class StrongPasswordAttribute : ValidationAttribute
{
    private readonly int _minLength;
    private readonly bool _requireUppercase;
    private readonly bool _requireLowercase;
    private readonly bool _requireDigit;
    private readonly bool _requireSpecialChar;

    /// <summary>
    /// Initializes the strong password validation with configurable requirements.
    /// </summary>
    /// <param name="minLength">Minimum password length</param>
    /// <param name="requireUppercase">Whether uppercase letters are required</param>
    /// <param name="requireLowercase">Whether lowercase letters are required</param>
    /// <param name="requireDigit">Whether digits are required</param>
    /// <param name="requireSpecialChar">Whether special characters are required</param>
    public StrongPasswordAttribute(
        int minLength = 8,
        bool requireUppercase = true,
        bool requireLowercase = true,
        bool requireDigit = true,
        bool requireSpecialChar = true)
    {
        _minLength = minLength;
        _requireUppercase = requireUppercase;
        _requireLowercase = requireLowercase;
        _requireDigit = requireDigit;
        _requireSpecialChar = requireSpecialChar;

        ErrorMessage = BuildErrorMessage();
    }

    /// <summary>
    /// Validates password strength against configured requirements.
    /// </summary>
    public override bool IsValid(object? value)
    {
        if (value is not string password)
        {
            return false;
        }

        // Check minimum length
        if (password.Length < _minLength)
        {
            return false;
        }

        // Check character requirements
        if (_requireUppercase && !password.Any(char.IsUpper))
        {
            return false;
        }

        if (_requireLowercase && !password.Any(char.IsLower))
        {
            return false;
        }

        if (_requireDigit && !password.Any(char.IsDigit))
        {
            return false;
        }

        if (_requireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return false;
        }

        // Check for common weak patterns
        if (IsCommonWeakPassword(password))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds a descriptive error message based on password requirements.
    /// </summary>
    private string BuildErrorMessage()
    {
        var requirements = new List<string>();

        requirements.Add($"at least {_minLength} characters");

        if (_requireUppercase) requirements.Add("one uppercase letter");
        if (_requireLowercase) requirements.Add("one lowercase letter");
        if (_requireDigit) requirements.Add("one number");
        if (_requireSpecialChar) requirements.Add("one special character");

        return $"Password must contain {string.Join(", ", requirements)}";
    }

    /// <summary>
    /// Checks for common weak password patterns that should be rejected.
    /// </summary>
    private static bool IsCommonWeakPassword(string password)
    {
        var commonPatterns = new[]
        {
            @"^(.)\1+$", // All same character
            @"^(012|123|234|345|456|567|678|789|890|987|876|765|654|543|432|321|210)", // Sequential numbers
            @"^(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", // Sequential letters
        };

        var lowerPassword = password.ToLowerInvariant();

        foreach (var pattern in commonPatterns)
        {
            if (Regex.IsMatch(lowerPassword, pattern))
            {
                return true;
            }
        }

        // Check for common weak passwords
        var commonWeakPasswords = new[]
        {
            "password", "123456", "password123", "admin", "letmein",
            "welcome", "monkey", "dragon", "qwerty", "abc123"
        };

        return commonWeakPasswords.Contains(lowerPassword);
    }
}