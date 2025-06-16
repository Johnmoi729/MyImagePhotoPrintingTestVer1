using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.DTOs.Common;
using MyImage.Application.Interfaces;

namespace MyImage.API.Controllers;

/// <summary>
/// Authentication controller handling user registration, login, and token management.
/// This controller provides secure endpoints for all authentication operations in the MyImage system.
/// It implements proper HTTP status codes, input validation, and security measures including rate limiting
/// to protect against common authentication attacks while providing a clean REST API interface.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Authentication")]
[ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes the authentication controller with required services.
    /// Sets up dependency injection for authentication business logic and logging capabilities.
    /// The controller acts as a thin layer over the authentication service, handling HTTP concerns
    /// while delegating business logic to the service layer.
    /// </summary>
    /// <param name="authenticationService">Service handling authentication business logic</param>
    /// <param name="logger">Logger for HTTP request tracking and security monitoring</param>
    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// This endpoint validates user credentials and returns JWT tokens for session management.
    /// It implements security best practices including rate limiting and secure token generation
    /// while providing clear feedback for authentication failures without exposing sensitive information.
    /// </summary>
    /// <param name="loginRequest">User credentials and login preferences</param>
    /// <returns>Authentication tokens and user information if successful</returns>
    /// <response code="200">Login successful with authentication tokens</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Invalid credentials or account issues</response>
    /// <response code="429">Too many login attempts - rate limit exceeded</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Login([FromBody] LoginRequestDto loginRequest)
    {
        try
        {
            _logger.LogInformation("Login attempt from IP: {IP} for email: {Email}",
                Request.HttpContext.Connection.RemoteIpAddress, loginRequest.Email);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Login validation failed for email: {Email}, Errors: {Errors}",
                    loginRequest.Email, string.Join(", ", errors));

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Attempt authentication
            var authResponse = await _authenticationService.LoginAsync(loginRequest);

            if (authResponse == null)
            {
                _logger.LogWarning("Login failed for email: {Email} from IP: {IP}",
                    loginRequest.Email, Request.HttpContext.Connection.RemoteIpAddress);

                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = new List<ErrorDto> { new() { Message = "Authentication failed" } }
                });
            }

            _logger.LogInformation("Login successful for user: {UserId}, Email: {Email}",
                authResponse.UserId, authResponse.Email);

            return Ok(new ApiResponseDto<AuthResponseDto>
            {
                Success = true,
                Data = authResponse,
                Message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for email: {Email}", loginRequest.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Registers a new user account with email verification.
    /// This endpoint creates new user accounts with comprehensive validation and security measures.
    /// It handles password complexity validation, email uniqueness checking, and initiates the
    /// email verification process while immediately providing authentication tokens for a smooth user experience.
    /// </summary>
    /// <param name="registerRequest">Complete registration information including personal details</param>
    /// <returns>Authentication tokens and user information if registration successful</returns>
    /// <response code="201">Registration successful with authentication tokens</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="409">Email address already registered</response>
    /// <response code="429">Too many registration attempts - rate limit exceeded</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponseDto<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Register([FromBody] RegisterRequestDto registerRequest)
    {
        try
        {
            _logger.LogInformation("Registration attempt from IP: {IP} for email: {Email}",
                Request.HttpContext.Connection.RemoteIpAddress, registerRequest.Email);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Registration validation failed for email: {Email}, Errors: {Errors}",
                    registerRequest.Email, string.Join(", ", errors));

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Attempt registration
            var authResponse = await _authenticationService.RegisterAsync(registerRequest);

            if (authResponse == null)
            {
                _logger.LogWarning("Registration failed for email: {Email} from IP: {IP}",
                    registerRequest.Email, Request.HttpContext.Connection.RemoteIpAddress);

                return Conflict(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email address is already registered",
                    Errors = new List<ErrorDto> { new() { Field = "email", Message = "Email address already exists" } }
                });
            }

            _logger.LogInformation("Registration successful for user: {UserId}, Email: {Email}",
                authResponse.UserId, authResponse.Email);

            return CreatedAtAction(nameof(Register), new ApiResponseDto<AuthResponseDto>
            {
                Success = true,
                Data = authResponse,
                Message = "Registration successful. Please check your email for verification instructions."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration attempt for email: {Email}", registerRequest.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Refreshes access tokens using a valid refresh token.
    /// This endpoint implements the secure token refresh mechanism that maintains user sessions
    /// without requiring frequent re-authentication. It validates refresh tokens and issues new
    /// access tokens while maintaining security boundaries and session continuity.
    /// </summary>
    /// <param name="refreshRequest">Refresh token to validate and exchange</param>
    /// <returns>New authentication tokens if refresh successful</returns>
    /// <response code="200">Token refresh successful with new tokens</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponseDto<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto refreshRequest)
    {
        try
        {
            _logger.LogDebug("Token refresh attempt from IP: {IP}", Request.HttpContext.Connection.RemoteIpAddress);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Attempt token refresh
            var authResponse = await _authenticationService.RefreshTokenAsync(refreshRequest);

            if (authResponse == null)
            {
                _logger.LogWarning("Token refresh failed from IP: {IP}", Request.HttpContext.Connection.RemoteIpAddress);

                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token",
                    Errors = new List<ErrorDto> { new() { Message = "Token refresh failed" } }
                });
            }

            _logger.LogDebug("Token refresh successful for user: {UserId}", authResponse.UserId);

            return Ok(new ApiResponseDto<AuthResponseDto>
            {
                Success = true,
                Data = authResponse,
                Message = "Token refreshed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during token refresh",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Logs out the current user and invalidates their session tokens.
    /// This endpoint ensures secure logout by revoking refresh tokens and cleaning up session data.
    /// It requires authentication to identify which user session to terminate and provides
    /// proper cleanup for security on shared devices.
    /// </summary>
    /// <returns>Confirmation of successful logout</returns>
    /// <response code="200">Logout successful</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<object>>> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "User not authenticated",
                    Errors = new List<ErrorDto> { new() { Message = "Invalid authentication" } }
                });
            }

            _logger.LogInformation("Logout request for user: {UserId}", userId);

            // Extract refresh token from request header or body if available
            var refreshToken = Request.Headers["X-Refresh-Token"].FirstOrDefault() ?? string.Empty;

            // Perform logout
            var success = await _authenticationService.LogoutAsync(userId, refreshToken);

            if (!success)
            {
                _logger.LogWarning("Logout failed for user: {UserId}", userId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Logout failed",
                    Errors = new List<ErrorDto> { new() { Message = "Unable to complete logout" } }
                });
            }

            _logger.LogInformation("Logout successful for user: {UserId}", userId);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Logout successful",
                Data = new { LoggedOut = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during logout",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Initiates the password reset process by sending a secure reset link via email.
    /// This endpoint generates time-limited tokens and sends them to verified email addresses.
    /// For security, the response doesn't indicate whether the email exists in the system,
    /// preventing email enumeration attacks while still providing users with recovery options.
    /// </summary>
    /// <param name="forgotPasswordRequest">Email address for password reset</param>
    /// <returns>Confirmation that reset process was initiated</returns>
    /// <response code="200">Password reset process initiated</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="429">Too many reset attempts - rate limit exceeded</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponseDto<object>>> ForgotPassword([FromBody] ForgotPasswordRequestDto forgotPasswordRequest)
    {
        try
        {
            _logger.LogInformation("Password reset request from IP: {IP} for email: {Email}",
                Request.HttpContext.Connection.RemoteIpAddress, forgotPasswordRequest.Email);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Initiate password reset (always returns true for security)
            await _authenticationService.ForgotPasswordAsync(forgotPasswordRequest);

            _logger.LogInformation("Password reset process initiated for email: {Email}", forgotPasswordRequest.Email);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "If an account with that email exists, you will receive password reset instructions.",
                Data = new { EmailSent = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request for email: {Email}", forgotPasswordRequest.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Completes the password reset process using a token from the reset email.
    /// This endpoint validates reset tokens, ensures they haven't expired, and updates passwords
    /// with proper hashing. Tokens are invalidated after use to prevent replay attacks,
    /// providing a secure password recovery mechanism.
    /// </summary>
    /// <param name="resetPasswordRequest">Reset token and new password information</param>
    /// <returns>Confirmation of successful password reset</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Invalid or expired reset token</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<object>>> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequest)
    {
        try
        {
            _logger.LogInformation("Password reset completion attempt from IP: {IP}",
                Request.HttpContext.Connection.RemoteIpAddress);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Attempt password reset
            var success = await _authenticationService.ResetPasswordAsync(resetPasswordRequest);

            if (!success)
            {
                _logger.LogWarning("Password reset failed - invalid or expired token from IP: {IP}",
                    Request.HttpContext.Connection.RemoteIpAddress);

                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid or expired reset token",
                    Errors = new List<ErrorDto> { new() { Message = "Password reset failed" } }
                });
            }

            _logger.LogInformation("Password reset completed successfully from IP: {IP}",
                Request.HttpContext.Connection.RemoteIpAddress);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Password reset successful. You can now login with your new password.",
                Data = new { PasswordReset = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset completion");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while resetting your password",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Allows authenticated users to change their password with current password verification.
    /// This endpoint provides an additional security layer by requiring current password validation
    /// before allowing changes, protecting against unauthorized password changes even if a
    /// user's session is compromised but their original password remains secure.
    /// </summary>
    /// <param name="changePasswordRequest">Current and new password information</param>
    /// <returns>Confirmation of successful password change</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Invalid current password or user not authenticated</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<object>>> ChangePassword([FromBody] ChangePasswordRequestDto changePasswordRequest)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "User not authenticated",
                    Errors = new List<ErrorDto> { new() { Message = "Invalid authentication" } }
                });
            }

            _logger.LogInformation("Password change request for user: {UserId}", userId);

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors.Select(e => new ErrorDto { Message = e }).ToList()
                });
            }

            // Attempt password change
            var success = await _authenticationService.ChangePasswordAsync(userId, changePasswordRequest);

            if (!success)
            {
                _logger.LogWarning("Password change failed for user: {UserId} - invalid current password", userId);

                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Current password is incorrect",
                    Errors = new List<ErrorDto> { new() { Field = "currentPassword", Message = "Invalid current password" } }
                });
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Password changed successfully",
                Data = new { PasswordChanged = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while changing your password",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Verifies a user's email address using a token from the verification email.
    /// This endpoint completes the email verification process by validating verification tokens
    /// and marking email addresses as verified, which may unlock additional features or
    /// remove account limitations based on business rules.
    /// </summary>
    /// <param name="token">Email verification token from verification email</param>
    /// <returns>Confirmation of successful email verification</returns>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or missing token</response>
    /// <response code="401">Invalid or expired verification token</response>
    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponseDto<object>>> VerifyEmail([FromQuery] string token)
    {
        try
        {
            _logger.LogInformation("Email verification attempt from IP: {IP}",
                Request.HttpContext.Connection.RemoteIpAddress);

            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Verification token is required",
                    Errors = new List<ErrorDto> { new() { Field = "token", Message = "Token is required" } }
                });
            }

            // Attempt email verification
            var success = await _authenticationService.VerifyEmailAsync(token);

            if (!success)
            {
                _logger.LogWarning("Email verification failed - invalid token from IP: {IP}",
                    Request.HttpContext.Connection.RemoteIpAddress);

                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid or expired verification token",
                    Errors = new List<ErrorDto> { new() { Message = "Email verification failed" } }
                });
            }

            _logger.LogInformation("Email verification completed successfully from IP: {IP}",
                Request.HttpContext.Connection.RemoteIpAddress);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "Email verified successfully",
                Data = new { EmailVerified = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while verifying your email",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }

    /// <summary>
    /// Resends the email verification message for users who didn't receive the original.
    /// This endpoint generates new verification tokens and sends fresh verification emails
    /// while implementing rate limiting to prevent abuse. It provides a way for users to
    /// recover from email delivery issues during the registration process.
    /// </summary>
    /// <param name="email">Email address that needs verification</param>
    /// <returns>Confirmation that verification email was sent</returns>
    /// <response code="200">Verification email sent</response>
    /// <response code="400">Invalid email address</response>
    /// <response code="429">Too many verification attempts - rate limit exceeded</response>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponseDto<object>>> ResendEmailVerification([FromBody] string email)
    {
        try
        {
            _logger.LogInformation("Email verification resend request from IP: {IP} for email: {Email}",
                Request.HttpContext.Connection.RemoteIpAddress, email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email address is required",
                    Errors = new List<ErrorDto> { new() { Field = "email", Message = "Email is required" } }
                });
            }

            // Resend verification email (always returns true for security)
            await _authenticationService.ResendEmailVerificationAsync(email);

            _logger.LogInformation("Email verification resend completed for email: {Email}", email);

            return Ok(new ApiResponseDto<object>
            {
                Success = true,
                Message = "If an unverified account with that email exists, a new verification email has been sent.",
                Data = new { EmailSent = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification resend for email: {Email}", email);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while sending verification email",
                Errors = new List<ErrorDto> { new() { Message = "Internal server error" } }
            });
        }
    }
}