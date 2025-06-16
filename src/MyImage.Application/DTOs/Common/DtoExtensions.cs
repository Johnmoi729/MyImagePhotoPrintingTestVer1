using AutoMapper;
using MyImage.Core.Entities;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Auth;

namespace MyImage.Application.DTOs.Common;

/// <summary>
/// Extension methods for common DTO operations.
/// Provides utility methods for working with DTOs and API responses
/// to reduce code duplication and improve consistency across the application.
/// </summary>
public static class DtoExtensions
{
    /// <summary>
    /// Converts a regular enumerable to a paginated result DTO.
    /// Useful for in-memory pagination or when working with pre-filtered data.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    /// <param name="items">Items to paginate</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <returns>Paginated result with proper pagination metadata</returns>
    public static PagedResultDto<T> ToPagedResult<T>(this IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Creates a successful API response with data.
    /// Convenience method for controller actions that return successful results.
    /// </summary>
    /// <typeparam name="T">Type of the response data</typeparam>
    /// <param name="data">Data to include in the response</param>
    /// <param name="message">Optional success message</param>
    /// <returns>Successful API response DTO</returns>
    public static ApiResponseDto<T> ToSuccessResponse<T>(this T data, string message = "Operation completed successfully")
    {
        return ApiResponseDto<T>.CreateSuccess(data, message);
    }

    /// <summary>
    /// Creates an error API response with details.
    /// Convenience method for controller actions that need to return error responses.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Optional collection of detailed error information</param>
    /// <returns>Error API response DTO</returns>
    public static ApiResponseDto<object> ToErrorResponse(string message, List<ErrorDto>? errors = null)
    {
        return ApiResponseDto<object>.CreateError(message, errors);
    }

    /// <summary>
    /// Validates that pagination parameters are within acceptable ranges.
    /// Ensures page numbers and page sizes are positive and within reasonable limits.
    /// </summary>
    /// <param name="page">Page number to validate</param>
    /// <param name="pageSize">Page size to validate</param>
    /// <param name="maxPageSize">Maximum allowed page size</param>
    /// <returns>Tuple indicating validity and any error messages</returns>
    public static (bool IsValid, List<ErrorDto> Errors) ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 100)
    {
        var errors = new List<ErrorDto>();

        if (page < 1)
        {
            errors.Add(new ErrorDto { Field = "page", Message = "Page number must be greater than 0" });
        }

        if (pageSize < 1)
        {
            errors.Add(new ErrorDto { Field = "pageSize", Message = "Page size must be greater than 0" });
        }

        if (pageSize > maxPageSize)
        {
            errors.Add(new ErrorDto { Field = "pageSize", Message = $"Page size cannot exceed {maxPageSize}" });
        }

        return (!errors.Any(), errors);
    }
}