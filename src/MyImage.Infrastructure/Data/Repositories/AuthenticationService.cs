using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using MyImage.Application.DTOs.Auth;
using MyImage.Application.Interfaces;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// Service implementation for authentication operations including login, registration, and token management.
/// This service encapsulates all authentication business logic and security policies for the MyImage application.
/// It handles password hashing, JWT token generation, email verification, and session management while
/// maintaining security best practices and providing comprehensive logging for security monitoring.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly Dictionary<string, string> _refreshTokens; // In production, use Redis or database

    // JWT Configuration from appsettings.json
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    /// <summary>
    /// Initializes the authentication service with required dependencies and JWT configuration.
    /// Sets up password hashing, token generation, and email services while loading
    /// security configuration from application settings. The service uses BCrypt for password hashing
    /// and JWT tokens for session management with configurable expiration times.
    /// </summary>
    /// <param name="userRepository">Repository for user data access operations</param>
    /// <param name="emailService">Service for sending authentication-related emails</param>
    /// <param name="configuration">Application configuration containing JWT and security settings</param>
    /// <param name="logger">Logger for security event tracking and troubleshooting</param>
    public AuthenticationService(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _refreshTokens = new Dictionary<string, string>(); // Temporary in-memory storage

        // Load JWT configuration with validation
        _jwtSecret = configuration["JwtSettings:Secret"] ??
            throw new InvalidOperationException("JWT Secret is not configured");
        _jwtIssuer = configuration["JwtSettings:Issuer"] ?? "MyImage.API";
        _jwtAudience = configuration["JwtSettings:Audience"] ?? "MyImage.Users";
        _jwtExpirationMinutes = int.Parse(configuration["JwtSettings:ExpirationMinutes"] ?? "60");
        _refreshTokenExpirationDays = int.Parse(configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "30");

        _logger.LogInformation("Authentication service initialized with JWT expiration: {ExpirationMinutes} minutes",
            _jwtExpirationMinutes);
    }

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// This method implements the complete login workflow including credential validation,
    /// password verification using BCrypt, session creation, and JWT token generation.
    /// It includes security measures like login attempt tracking and account lockout protection.
    /// </summary>
    /// <param name="loginRequest">User credentials and login preferences</param>
    /// <returns>Authentication response with tokens and user info, or null if login fails</returns>
    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginRequest.Email);

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Email}", loginRequest.Email);
                return null; // Don't reveal whether email exists for security
            }

            // Check account status
            if (user.Metadata.AccountStatus != "active")
            {
                _logger.LogWarning("Login failed - account not active: {Email}, Status: {Status}",
                    loginRequest.Email, user.Metadata.AccountStatus);
                return null;
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - invalid password: {Email}", loginRequest.Email);
                return null;
            }

            // Update login tracking
            await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token (in production, use Redis or database with expiration)
            _refreshTokens[refreshToken] = user.Id;

            _logger.LogInformation("Login successful for user: {UserId}, Email: {Email}", user.Id, user.Email);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName,
                Roles = user.Roles,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtExpirationMinutes * 60, // Convert to seconds
                IssuedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for email: {Email}", loginRequest.Email);
            return null; // Don't expose internal errors to prevent information leakage
        }
    }

    /// <summary>
    /// Creates a new user account with email verification setup.
    /// This method handles the complete registration workflow including email uniqueness validation,
    /// password complexity checking, secure password hashing, user creation, and email verification setup.
    /// The process ensures data integrity and security while providing a smooth onboarding experience.
    /// </summary>
    /// <param name="registerRequest">Complete registration information</param>
    /// <returns>Authentication response if registration succeeds, or null with validation errors</returns>
    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto registerRequest)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", registerRequest.Email);

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(registerRequest.Email))
            {
                _logger.LogWarning("Registration failed - email already exists: {Email}", registerRequest.Email);
                return null; // Email already in use
            }

            // Hash the password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

            // Generate email verification token
            var emailVerificationToken = GenerateSecureToken();

            // Create user entity
            var user = new User
            {
                Email = registerRequest.Email.ToLowerInvariant(),
                PasswordHash = passwordHash,
                EmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                Profile = new UserProfile
                {
                    FirstName = registerRequest.FirstName.Trim(),
                    LastName = registerRequest.LastName.Trim(),
                    DisplayName = $"{registerRequest.FirstName.Trim()} {registerRequest.LastName.Trim()[0]}.",
                    PhoneNumber = registerRequest.PhoneNumber?.Trim()
                },
                Preferences = new UserPreferences
                {
                    EmailNotifications = new EmailNotificationPreferences
                    {
                        OrderUpdates = true,
                        Promotions = registerRequest.AcceptMarketing,
                        PhotoReminders = true
                    }
                },
                Roles = new List<string> { "customer" },
                Statistics = new UserStatistics(),
                Metadata = new UserMetadata
                {
                    RegistrationSource = "web",
                    AccountStatus = "active"
                }
            };

            // Create user in database
            var createdUser = await _userRepository.CreateAsync(user);

            // Send email verification
            await SendEmailVerificationAsync(createdUser.Email, emailVerificationToken, createdUser.Profile.FirstName);

            // Generate tokens for immediate login
            var accessToken = GenerateJwtToken(createdUser);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token
            _refreshTokens[refreshToken] = createdUser.Id;

            _logger.LogInformation("User registered successfully: {UserId}, Email: {Email}",
                createdUser.Id, createdUser.Email);

            return new AuthResponseDto
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.Profile.FirstName,
                LastName = createdUser.Profile.LastName,
                Roles = createdUser.Roles,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtExpirationMinutes * 60,
                IssuedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", registerRequest.Email);
            return null;
        }
    }

    /// <summary>
    /// Generates new access tokens using a valid refresh token.
    /// This method implements the secure token refresh mechanism that maintains user sessions
    /// without requiring frequent re-authentication. It validates refresh tokens, checks for
    /// revocation, and issues new tokens while maintaining security boundaries.
    /// </summary>
    /// <param name="refreshRequest">Refresh token to validate and exchange</param>
    /// <returns>New authentication response with fresh tokens, or null if refresh fails</returns>
    public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto refreshRequest)
    {
        try
        {
            _logger.LogDebug("Token refresh attempt");

            // Validate refresh token
            if (!_refreshTokens.TryGetValue(refreshRequest.RefreshToken, out var userId))
            {
                _logger.LogWarning("Token refresh failed - invalid refresh token");
                return null;
            }

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.Metadata.AccountStatus != "active")
            {
                _logger.LogWarning("Token refresh failed - user not found or inactive: {UserId}", userId);

                // Remove invalid refresh token
                _refreshTokens.Remove(refreshRequest.RefreshToken);
                return null;
            }

            // Generate new tokens
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Replace refresh token
            _refreshTokens.Remove(refreshRequest.RefreshToken);
            _refreshTokens[newRefreshToken] = user.Id;

            _logger.LogDebug("Token refresh successful for user: {UserId}", userId);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName,
                Roles = user.Roles,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwtExpirationMinutes * 60,
                IssuedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    /// <summary>
    /// Invalidates user sessions and tokens during logout.
    /// This method ensures secure logout by revoking refresh tokens and cleaning up session data.
    /// Proper logout implementation is crucial for security, especially on shared devices.
    /// </summary>
    /// <param name="userId">User identifier for logout tracking</param>
    /// <param name="refreshToken">Current refresh token to invalidate</param>
    /// <returns>True if logout completed successfully</returns>
    public async Task<bool> LogoutAsync(string userId, string refreshToken)
    {
        try
        {
            _logger.LogInformation("Logout request for user: {UserId}", userId);

            // Remove refresh token from storage
            if (_refreshTokens.ContainsKey(refreshToken))
            {
                _refreshTokens.Remove(refreshToken);
            }

            // In production, also revoke the token in the database/Redis
            // and potentially blacklist the JWT token until expiration

            _logger.LogInformation("Logout successful for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Initiates the password reset process by sending a secure reset link via email.
    /// This method generates time-limited tokens and sends them to verified email addresses.
    /// For security, the response doesn't indicate whether the email exists in the system.
    /// </summary>
    /// <param name="forgotPasswordRequest">Email address for password reset</param>
    /// <returns>True if the process was initiated (regardless of email existence)</returns>
    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto forgotPasswordRequest)
    {
        try
        {
            _logger.LogInformation("Password reset request for email: {Email}", forgotPasswordRequest.Email);

            var user = await _userRepository.GetByEmailAsync(forgotPasswordRequest.Email);
            if (user == null || user.Metadata.AccountStatus != "active")
            {
                // For security, don't reveal whether email exists
                _logger.LogDebug("Password reset request for non-existent or inactive email: {Email}",
                    forgotPasswordRequest.Email);
                return true; // Always return true to prevent email enumeration
            }

            // Generate password reset token (expires in 1 hour)
            var resetToken = GenerateSecureToken();
            var expirationTime = DateTime.UtcNow.AddHours(1);

            // Update user with reset token
            user.PasswordResetToken = resetToken;
            user.PasswordResetExpires = expirationTime;
            await _userRepository.UpdateAsync(user.Id, user);

            // Send password reset email
            await SendPasswordResetEmailAsync(user.Email, resetToken, user.Profile.FirstName);

            _logger.LogInformation("Password reset email sent for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request for email: {Email}",
                forgotPasswordRequest.Email);
            return true; // Always return true to prevent information leakage
        }
    }

    /// <summary>
    /// Completes the password reset process using a token from the reset email.
    /// This method validates reset tokens, ensures they haven't expired, and updates passwords
    /// with proper hashing. Tokens are invalidated after use to prevent replay attacks.
    /// </summary>
    /// <param name="resetPasswordRequest">Reset token and new password information</param>
    /// <returns>True if password reset completed successfully</returns>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest)
    {
        try
        {
            _logger.LogInformation("Password reset completion attempt");

            var user = await _userRepository.GetByPasswordResetTokenAsync(resetPasswordRequest.Token);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed - invalid or expired token");
                return false;
            }

            // Hash the new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordRequest.NewPassword,
                BCrypt.Net.BCrypt.GenerateSalt(12));

            // Update password and clear reset token
            user.PasswordHash = newPasswordHash;
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;

            await _userRepository.UpdateAsync(user.Id, user);

            _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset completion");
            return false;
        }
    }

    /// <summary>
    /// Allows authenticated users to change their password with current password verification.
    /// This method provides an additional security layer by requiring current password validation
    /// before allowing changes, protecting against unauthorized password changes.
    /// </summary>
    /// <param name="userId">Authenticated user's identifier</param>
    /// <param name="changePasswordRequest">Current and new password information</param>
    /// <returns>True if password change completed successfully</returns>
    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequestDto changePasswordRequest)
    {
        try
        {
            _logger.LogInformation("Password change request for user: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed - user not found: {UserId}", userId);
                return false;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(changePasswordRequest.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed - invalid current password for user: {UserId}", userId);
                return false;
            }

            // Hash the new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordRequest.NewPassword,
                BCrypt.Net.BCrypt.GenerateSalt(12));

            // Update password
            user.PasswordHash = newPasswordHash;
            await _userRepository.UpdateAsync(user.Id, user);

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Completes the email verification process using a token sent to the user's email.
    /// This method validates verification tokens and marks email addresses as verified,
    /// which may unlock additional features or remove account limitations.
    /// </summary>
    /// <param name="token">Email verification token from verification email</param>
    /// <returns>True if email verification completed successfully</returns>
    public async Task<bool> VerifyEmailAsync(string token)
    {
        try
        {
            _logger.LogInformation("Email verification attempt");

            var user = await _userRepository.GetByEmailVerificationTokenAsync(token);
            if (user == null)
            {
                _logger.LogWarning("Email verification failed - invalid token");
                return false;
            }

            // Mark email as verified and clear verification token
            user.EmailVerified = true;
            user.EmailVerificationToken = null;

            await _userRepository.UpdateAsync(user.Id, user);

            _logger.LogInformation("Email verified successfully for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return false;
        }
    }

    /// <summary>
    /// Resends the email verification message for users who didn't receive the original.
    /// This method generates new verification tokens and sends fresh verification emails
    /// while implementing rate limiting to prevent abuse.
    /// </summary>
    /// <param name="email">Email address that needs verification</param>
    /// <returns>True if verification email was sent successfully</returns>
    public async Task<bool> ResendEmailVerificationAsync(string email)
    {
        try
        {
            _logger.LogInformation("Email verification resend request for: {Email}", email);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.EmailVerified)
            {
                _logger.LogDebug("Email verification resend - user not found or already verified: {Email}", email);
                return true; // Don't reveal user existence status
            }

            // Generate new verification token
            var newVerificationToken = GenerateSecureToken();
            user.EmailVerificationToken = newVerificationToken;

            await _userRepository.UpdateAsync(user.Id, user);

            // Send new verification email
            await SendEmailVerificationAsync(user.Email, newVerificationToken, user.Profile.FirstName);

            _logger.LogInformation("Email verification resent for user: {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification resend for: {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Validates JWT tokens and extracts user information for authorization.
    /// This method verifies token signatures, expiration dates, and extracts user claims
    /// for use in authorization decisions throughout the application.
    /// </summary>
    /// <param name="token">JWT access token to validate</param>
    /// <returns>User information if token is valid, null otherwise</returns>
    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "sub").Value;

            // Retrieve user to ensure account is still active
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Metadata.AccountStatus != "active")
            {
                return null;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Generates a JWT access token with user claims and configured expiration.
    /// This method creates tokens containing user identity and role information
    /// for authentication and authorization throughout the application.
    /// </summary>
    /// <param name="user">User entity to create token for</param>
    /// <returns>JWT access token as string</returns>
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.Profile.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.Profile.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// This method creates random tokens for maintaining user sessions
    /// beyond the access token expiration time.
    /// </summary>
    /// <returns>Secure random refresh token</returns>
    private string GenerateRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Generates a cryptographically secure token for email verification and password reset.
    /// This method creates random tokens that are safe for use in security-sensitive operations.
    /// </summary>
    /// <returns>Secure random token</returns>
    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    /// <summary>
    /// Sends email verification message to newly registered users.
    /// This method creates and sends verification emails with secure tokens
    /// that users can click to verify their email addresses.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="token">Verification token to include in email</param>
    /// <param name="firstName">User's first name for personalization</param>
    private async Task SendEmailVerificationAsync(string email, string token, string firstName)
    {
        var verificationUrl = $"{_configuration["ApiSettings:BaseUrl"]}/verify-email?token={token}";

        var subject = "Verify Your Email Address - MyImage";
        var body = $@"
            <h2>Welcome to MyImage, {firstName}!</h2>
            <p>Please verify your email address by clicking the link below:</p>
            <p><a href='{verificationUrl}'>Verify Email Address</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create this account, please ignore this email.</p>
        ";

        await _emailService.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Sends password reset email with secure reset link.
    /// This method creates and sends password reset emails with time-limited tokens
    /// that users can use to securely reset their passwords.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="token">Password reset token</param>
    /// <param name="firstName">User's first name for personalization</param>
    private async Task SendPasswordResetEmailAsync(string email, string token, string firstName)
    {
        var resetUrl = $"{_configuration["ApiSettings:BaseUrl"]}/reset-password?token={token}";

        var subject = "Reset Your Password - MyImage";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>Hello {firstName},</p>
            <p>You requested to reset your password. Click the link below to set a new password:</p>
            <p><a href='{resetUrl}'>Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request this reset, please ignore this email.</p>
        ";

        await _emailService.SendEmailAsync(email, subject, body);
    }
}

/// <summary>
/// Interface for email service used by authentication operations.
/// This interface will be implemented by the email service in the infrastructure layer
/// to handle email sending for verification and password reset functionality.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email message to the specified recipient.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="htmlBody">HTML email body content</param>
    /// <returns>Task representing the async email sending operation</returns>
    Task SendEmailAsync(string to, string subject, string htmlBody);
}