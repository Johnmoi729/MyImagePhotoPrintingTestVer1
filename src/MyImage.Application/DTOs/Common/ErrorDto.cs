using AutoMapper;
using MyImage.Core.Entities;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Auth;

namespace MyImage.Application.DTOs.Common;

/// <summary>
/// Detailed error information for API responses.
/// This DTO provides structured error details that can be used for field-level validation,
/// debugging, and providing specific feedback to users about what went wrong.
/// </summary>
public class ErrorDto
{
    /// <summary>
    /// The specific field or property that caused the error.
    /// Used for form validation and highlighting problematic input fields.
    /// Can be null for general errors not related to specific fields.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Human-readable error message describing what went wrong.
    /// Should be clear and actionable, suitable for displaying to end users.
    /// Contains specific information about how to fix the issue when possible.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional error code for programmatic error handling.
    /// Allows frontend applications to handle specific error types differently.
    /// Useful for internationalization and custom error handling logic.
    /// </summary>
    public string? Code { get; set; }
}
