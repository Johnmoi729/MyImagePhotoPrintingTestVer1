using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Validation;

/// <summary>
/// Custom validation attribute for comma-separated tag lists.
/// This attribute validates tag format, length, and content to ensure
/// proper tag organization and prevent abuse of the tagging system.
/// </summary>
public class ValidTagsAttribute : ValidationAttribute
{
    private readonly int _maxTags;
    private readonly int _maxTagLength;
    private readonly int _minTagLength;

    /// <summary>
    /// Initializes tag validation with configurable limits.
    /// </summary>
    /// <param name="maxTags">Maximum number of tags allowed</param>
    /// <param name="maxTagLength">Maximum length per tag</param>
    /// <param name="minTagLength">Minimum length per tag</param>
    public ValidTagsAttribute(int maxTags = 10, int maxTagLength = 30, int minTagLength = 2)
    {
        _maxTags = maxTags;
        _maxTagLength = maxTagLength;
        _minTagLength = minTagLength;

        ErrorMessage = $"Tags must be between {_minTagLength} and {_maxTagLength} characters, maximum {_maxTags} tags allowed";
    }

    /// <summary>
    /// Validates tag list format and content.
    /// </summary>
    public override bool IsValid(object? value)
    {
        if (value == null || value is not string tagsString)
        {
            return true; // Allow null/empty tags
        }

        if (string.IsNullOrWhiteSpace(tagsString))
        {
            return true;
        }

        var tags = tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrEmpty(tag))
            .ToList();

        // Check number of tags
        if (tags.Count > _maxTags)
        {
            return false;
        }

        // Check each tag
        foreach (var tag in tags)
        {
            if (tag.Length < _minTagLength || tag.Length > _maxTagLength)
            {
                return false;
            }

            // Check for invalid characters (only allow alphanumeric, spaces, hyphens, underscores)
            if (!Regex.IsMatch(tag, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                return false;
            }
        }

        return true;
    }
}