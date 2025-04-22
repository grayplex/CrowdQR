namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for login requests.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the username for login.
    /// </summary>
    public required string Username { get; set; }
}

/// <summary>
/// Data transfer object for authentication results.
/// </summary>
public class AuthResultDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the JWT token if authentication was successful.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user information.
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Gets or sets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}