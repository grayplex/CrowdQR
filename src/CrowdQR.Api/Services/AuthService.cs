using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrowdQR.Api.Services;

/// <summary>
/// Service for authentication and managing user sessions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthService"/> class.
/// </remarks>
/// <param name="context">The database context.</param>
/// <param name="configuration">The configuration.</param>
/// <param name="logger">The logger.</param>
/// <param name="passwordService">The password service.</param>
/// <param name="tokenService">The token service.</param>
/// <param name="emailService"> The email service.</param>
public class AuthService(
    CrowdQRContext context,
    IConfiguration configuration,
    ILogger<AuthService> logger,
    IPasswordService passwordService,
    ITokenService tokenService,
    IEmailService emailService) : IAuthService
{
    private readonly CrowdQRContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IEmailService _emailService = emailService;

    /// <summary>
    /// Authenticates a user by username or email and password.
    /// </summary>
    /// <param name="usernameOrEmail">The username or email to authenticate.</param>
    /// <param name="password">The password to verify (optional for audience users).</param>
    /// <returns>Authentication result with JWT token if successful, or error.</returns>
    public async Task<AuthResultDto> AuthenticateUser(string usernameOrEmail, string? password = null)
    {
        try
        {
            // Find user by username or email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            // If user doesn't exist, create a temporary one with audience role (only for username-based auth)
            if (user == null)
            {
                // Only create new audience users, not DJ users
                if (password != null)
                {
                    return new AuthResultDto
                    {
                        Success = false,
                        ErrorMessage = "Invalid username/email or password"
                    };
                }

                user = new User
                {
                    Username = usernameOrEmail,
                    Role = UserRole.Audience,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user {Username} with Audience role", usernameOrEmail);
            }
            else
            {
                // Check if this is a DJ account, which requires a password
                if (user.Role == UserRole.DJ)
                {
                    // DJ accounts must provide a password
                    if (string.IsNullOrEmpty(password))
                    {
                        return new AuthResultDto
                        {
                            Success = false,
                            ErrorMessage = "Password is required for DJ accounts"
                        };
                    }

                    // Verify password
                    if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt) ||
                        !_passwordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                    {
                        return new AuthResultDto
                        {
                            Success = false,
                            ErrorMessage = "Invalid username/email or password"
                        };
                    }

                    // TEMPORARILY DISABLED: Email Verification check
                    // Check if email is verified
                    /*
                    if (!user.IsEmailVerified)
                    {
                        return new AuthResultDto
                        {
                            Success = false,
                            ErrorMessage = "Please verify your email before logging in",
                            EmailVerificationRequired = true
                        };
                    }
                    */
                }
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    IsEmailVerified = user.IsEmailVerified,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user {UsernameOrEmail}", usernameOrEmail);
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Authentication failed"
            };
        }
    }

    /// <summary>
    /// Registers a new DJ user.
    /// </summary>
    /// <param name="registerDto">The registration data.</param>
    /// <returns>Registration result.</returns>
    public async Task<AuthResultDto> RegisterDj(AuthDjRegisterDto registerDto)
    {
        try
        {
            // Check if username is already taken
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Username is already taken"
                };
            }

            // Check if email is already taken
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Email is already registered"
                };
            }

            // Hash password
            var (hash, salt) = _passwordService.HashPassword(registerDto.Password);

            // Generate email verification token
            var token = _tokenService.GenerateVerificationToken();
            var tokenExpiry = _tokenService.GenerateTokenExpiry();

            // Create new user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.DJ,
                EmailVerificationToken = token,
                EmailTokenExpiry = tokenExpiry,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _emailService.SendVerificationEmailAsync(
                user.Email!,
                user.Username,
                token
            );

            return new AuthResultDto
            {
                Success = true,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    IsEmailVerified = false,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                },
                EmailVerificationRequired = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering DJ user {Username}", registerDto.Username);
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Registration failed"
            };
        }
    }

    /// <summary>
    /// Verifies a user's email.
    /// </summary>
    /// <param name="verifyEmailDto">The email verification data.</param>
    /// <returns>True if verification was successful, false otherwise.</returns>
    public async Task<bool> VerifyEmail(AuthVerifyEmailDto verifyEmailDto)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == verifyEmailDto.Email &&
                                          u.EmailVerificationToken == verifyEmailDto.Token);

            if (user == null)
            {
                return false;
            }

            // Check if token has expired
            if (user.EmailTokenExpiry < DateTime.UtcNow)
            {
                return false;
            }

            // Mark email as verified
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailTokenExpiry = null;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email during the verification process.");
            return false;
        }
    }

    /// <summary>
    /// Resends an email verification token.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ResendVerificationEmail(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.IsEmailVerified)
            {
                return false;
            }

            // Generate new token
            user.EmailVerificationToken = _tokenService.GenerateVerificationToken();
            user.EmailTokenExpiry = _tokenService.GenerateTokenExpiry();

            await _context.SaveChangesAsync();

            await _emailService.SendVerificationEmailAsync(
                user.Email!,
                user.Username,
                user.EmailVerificationToken!
            );

            return true;
        }
        catch (Exception ex)
        {
            var hashedEmail = Convert.ToBase64String(Encoding.UTF8.GetBytes(email));
            _logger.LogError(ex, "Error resending verification email to a user with hashed email: {HashedEmail}", hashedEmail);
            return false;
        }
    }

    /// <summary>
    /// Validates JWT token and returns the associated user.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The user if token is valid, null otherwise.</returns>
    public async Task<User?> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Use the EXACT same configuration as Program.cs JWT Bearer setup
            var jwtSecret = _configuration["JWT_SECRET"] ??
                           _configuration["Jwt:Secret"] ??
                           "test_jwt_secret_key_that_is_long_enough_for_testing_requirements_12345";


            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT_ISSUER"] ?? _configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
                ValidateAudience = true,
                ValidAudience = _configuration["JWT_AUDIENCE"] ?? _configuration["Jwt:Audience"] ?? "CrowdQR.Web",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Debug logging to verify configuration matches
            _logger.LogInformation("ValidateToken JWT Config - Secret: {SecretLength} chars, Issuer: {Issuer}, Audience: {Audience}",
                jwtSecret.Length,
                validationParameters.ValidIssuer,
                validationParameters.ValidAudience);

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await _context.Users.FindAsync(userId);
                _logger.LogInformation("Token validation successful for user ID: {UserId}", userId);
                return user;
            }

            _logger.LogWarning("Token validation failed: No valid user ID claim found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
                string.IsNullOrEmpty(user.PasswordSalt))
            {
                return false;
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return false;
            }

            // Hash new password
            var (hash, salt) = _passwordService.HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSecret = _configuration["JWT_SECRET"] ??
                   _configuration["Jwt:Secret"] ??
                   "test_jwt_secret_key_that_is_long_enough_for_testing_requirements_12345";

        // Ensure the secret is at least 32 characters
        if (jwtSecret.Length < 32)
        {
            throw new InvalidOperationException($"JWT secret must be at least 32 characters long. Current length: {jwtSecret.Length}");
        }

        var key = Encoding.UTF8.GetBytes(jwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("account_created", user.CreatedAt.ToString("O")),
            new("email_verified", user.IsEmailVerified.ToString().ToLower())
        };

        // Add email claim if available
        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
            Issuer = _configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
            Audience = _configuration["Jwt:Audience"] ?? "CrowdQR.Web",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        // Debug logging
        var logger = _logger;
        logger.LogInformation("AuthService JWT Config - Secret: {SecretLength} chars, Issuer: {Issuer}, Audience: {Audience}",
            jwtSecret.Length,
            _configuration["JWT_ISSUER"] ?? _configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
            _configuration["JWT_AUDIENCE"] ?? _configuration["Jwt:Audience"] ?? "CrowdQR.Web");

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Checks if a user can access a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can access the event, false otherwise.</returns>
    public async Task<bool> CanAccessEventAsync(int eventId, int userId)
    {
        var @event = await _context.Events.FindAsync(eventId);
        if (@event == null)
        {
            return false;
        }

        // DJ of the event can access it
        if (@event.DjUserId == userId)
        {
            return true;
        }

        // Check if user has a session for this event
        var hasSession = await _context.Sessions
            .AnyAsync(s => s.EventId == eventId && s.UserId == userId);

        return hasSession;
    }

    /// <summary>
    /// Checks if a user can modify a specific request.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can modify the request, false otherwise.</returns>
    public async Task<bool> CanModifyRequestAsync(int requestId, int userId)
    {
        var request = await _context.Requests
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null)
        {
            return false;
        }

        // Request owner can modify it
        if (request.UserId == userId)
        {
            return true;
        }

        // Event DJ can modify it
        if (request.Event.DjUserId == userId)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a user is a DJ.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user is a DJ, false otherwise.</returns>
    public async Task<bool> IsDjAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == UserRole.DJ;
    }

    /// <summary>
    /// Checks if a user needs to verify their email.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user needs to verify their email, false otherwise.</returns>
    public async Task<bool> NeedsEmailVerificationAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);

        // Only DJ accounts require email verification
        if (user == null || user.Role != UserRole.DJ)
        {
            return false;
        }

        return !user.IsEmailVerified;
    }
}