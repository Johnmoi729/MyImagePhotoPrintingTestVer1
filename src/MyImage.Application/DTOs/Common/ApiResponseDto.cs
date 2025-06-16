using AutoMapper;
using MyImage.Core.Entities;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Auth;

namespace MyImage.Application.DTOs.Common;

/// <summary>
/// Standardized API response wrapper for all endpoint responses.
/// This DTO ensures consistent response format across all API endpoints
/// with proper success/error indication, data payload, and error details.
/// It provides a unified structure for frontend applications to handle responses reliably.
/// </summary>
/// <typeparam name="T">Type of the data payload being returned</typeparam>
public class ApiResponseDto<T>
{
    /// <summary>
    /// Indicates whether the API operation was successful.
    /// True for successful operations, false for errors or failures.
    /// Frontend applications can use this flag for initial response handling logic.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The main data payload of the response.
    /// Contains the actual result data for successful operations.
    /// Will be null for error responses or operations that don't return data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the operation result.
    /// Provides context about what happened during the operation.
    /// Can be displayed to users or used for debugging and logging purposes.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Collection of error details for failed operations.
    /// Contains specific error information including field-level validation errors.
    /// Empty collection for successful operations, populated with details for failures.
    /// </summary>
    public List<ErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when the response was generated.
    /// Useful for debugging, caching decisions, and request tracking.
    /// Always set to UTC time for consistency across different time zones.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data payload.
    /// Convenience method for creating standardized success responses.
    /// </summary>
    /// <param name="data">Data to include in the response</param>
    /// <param name="message">Optional success message</param>
    /// <returns>Successful API response with data</returns>
    public static ApiResponseDto<T> CreateSuccess(T data, string message = "Operation completed successfully")
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response with error details.
    /// Convenience method for creating standardized error responses.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Optional collection of specific error details</param>
    /// <returns>Error API response</returns>
    public static ApiResponseDto<T> CreateError(string message, List<ErrorDto>? errors = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<ErrorDto> { new() { Message = message } }
        };
    }
}