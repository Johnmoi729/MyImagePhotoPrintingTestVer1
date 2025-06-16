using MyImage.Core.Entities;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.DTOs.Photos;
using MyImage.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace MyImage.Application.Interfaces;

/// <summary>
/// Service interface for authentication operations including login, registration, and session management.
/// This interface defines the contract for all authentication-related business logic in our application.
/// The implementation will handle password hashing, JWT token generation, email verification, and security policies.
/// By abstracting these operations into an interface, we can easily test our authentication logic and swap implementations if needed.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// This method orchestrates the complete login process including credential verification,
    /// session creation, and token generation. It validates the user's credentials against
    /// stored password hashes and creates the necessary tokens for maintaining user sessions.
    /// </summary>
    /// <param name="loginRequest">Contains email, password, and remember me preference</param>
    /// <returns>Authentication response with tokens and user information, or null if login fails</returns>
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto loginRequest);

    /// <summary>
    /// Creates a new user account with email verification.
    /// This method handles the complete registration workflow including duplicate email checking,
    /// password complexity validation, password hashing, user creation, and email verification setup.
    /// The business logic ensures data integrity and security while providing a smooth registration experience.
    /// </summary>
    /// <param name="registerRequest">Complete registration information including personal details</param>
    /// <returns>Authentication response if registration succeeds, or null with validation errors</returns>
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto registerRequest);

    /// <summary>
    /// Generates new access tokens using a valid refresh token.
    /// This method implements the secure token refresh mechanism that allows users to maintain
    /// their sessions without frequent re-authentication. It validates the refresh token,
    /// checks for revocation, and issues new tokens while maintaining security boundaries.
    /// </summary>
    /// <param name="refreshRequest">Contains the refresh token to be validated and exchanged</param>
    /// <returns>New authentication response with fresh tokens, or null if refresh fails</returns>
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto refreshRequest);

    /// <summary>
    /// Invalidates user sessions and tokens during logout.
    /// This method ensures secure logout by revoking refresh tokens and invalidating active sessions.
    /// Proper logout implementation is crucial for security, especially on shared devices,
    /// and helps prevent unauthorized access if devices are lost or compromised.
    /// </summary>
    /// <param name="userId">Unique identifier of the user logging out</param>
    /// <param name="refreshToken">Current refresh token to be invalidated</param>
    /// <returns>True if logout completed successfully</returns>
    Task<bool> LogoutAsync(string userId, string refreshToken);

    /// <summary>
    /// Initiates the password reset process by sending a secure reset link via email.
    /// This method generates a time-limited, single-use token and sends it to the user's email address.
    /// For security reasons, the response doesn't indicate whether the email exists in our system,
    /// preventing email enumeration attacks while still providing users with recovery options.
    /// </summary>
    /// <param name="forgotPasswordRequest">Contains the email address for password reset</param>
    /// <returns>True if the process was initiated (regardless of email existence)</returns>
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordRequest);

    /// <summary>
    /// Completes the password reset process using a token from the reset email.
    /// This method validates the reset token, ensures it hasn't expired, and updates the user's password
    /// with proper hashing. The token is invalidated after use to prevent replay attacks,
    /// and the user is optionally logged in with the new credentials for a seamless experience.
    /// </summary>
    /// <param name="resetPasswordRequest">Contains reset token and new password information</param>
    /// <returns>True if password reset completed successfully</returns>
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest);

    /// <summary>
    /// Allows authenticated users to change their password with current password verification.
    /// This method provides an additional security layer by requiring the current password before allowing changes.
    /// This protection is essential for preventing unauthorized password changes if a user's session
    /// is compromised but their original password remains secure.
    /// </summary>
    /// <param name="userId">Unique identifier of the authenticated user</param>
    /// <param name="changePasswordRequest">Contains current password and new password information</param>
    /// <returns>True if password change completed successfully</returns>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto changePasswordRequest);

    /// <summary>
    /// Completes the email verification process using a token sent to the user's email.
    /// This method validates the verification token and marks the user's email as verified,
    /// which may unlock additional features or remove account limitations.
    /// Email verification is crucial for ensuring users have access to their stated email address.
    /// </summary>
    /// <param name="token">Email verification token from the verification email</param>
    /// <returns>True if email verification completed successfully</returns>
    Task<bool> VerifyEmailAsync(string token);

    /// <summary>
    /// Resends the email verification message for users who didn't receive or lost the original.
    /// This method generates a new verification token and sends a fresh verification email,
    /// while invalidating any previous verification tokens for security.
    /// Rate limiting should be implemented to prevent email spam and abuse.
    /// </summary>
    /// <param name="email">Email address that needs verification</param>
    /// <returns>True if verification email was sent successfully</returns>
    Task<bool> ResendEmailVerificationAsync(string email);

    /// <summary>
    /// Validates JWT tokens and extracts user information for authorization.
    /// This method verifies token signatures, expiration dates, and extracts user claims
    /// for use in authorization decisions throughout the application.
    /// It's used by authentication middleware to validate incoming requests with bearer tokens.
    /// </summary>
    /// <param name="token">JWT access token to validate</param>
    /// <returns>User information if token is valid, null otherwise</returns>
    Task<User?> ValidateTokenAsync(string token);
}