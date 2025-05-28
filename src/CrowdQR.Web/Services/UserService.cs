using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing users through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the UserService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class UserService(ApiService apiService, ILogger<UserService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<UserService> _logger = logger;
    private const string BaseEndpoint = "api/user";

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>A list of users.</returns>
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _apiService.GetAsync<List<UserDto>>(BaseEndpoint);
        return users ?? [];
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user, or null if not found.</returns>
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        return await _apiService.GetAsync<UserDto>($"{BaseEndpoint}/{id}");
    }

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>The user, or null if not found.</returns>
    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        return await _apiService.GetAsync<UserDto>($"{BaseEndpoint}/username/{username}");
    }

    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    /// <param name="role">The role (0 = Audience, 1 = DJ).</param>
    /// <returns>A list of users with the specified role.</returns>
    public async Task<List<UserDto>> GetUsersByRoleAsync(int role)
    {
        var users = await _apiService.GetAsync<List<UserDto>>($"{BaseEndpoint}/role/{role}");
        return users ?? [];
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userDto">The user data.</param>
    /// <returns>The created user and success flag.</returns>
    public async Task<(bool Success, UserDto? User)> CreateUserAsync(UserCreateDto userDto)
    {
        var (success, response) = await _apiService.PostAsync<UserCreateDto, UserDto>(BaseEndpoint, userDto);
        if (!success)
        {
            _logger.LogError("Failed to create user");
            return (false, null);
        }
        if (response == null)
        {
            _logger.LogError("Failed to create user: No response received");
            return (false, null);
        }
        _logger.LogInformation("User created successfully: {UserId}", response.UserId);
        return (true, response);
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="userDto">The updated user data.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> UpdateUserAsync(int id, UserUpdateDto userDto)
    {
        return await _apiService.PutAsync($"{BaseEndpoint}/{id}", userDto);
    }

    /// <summary>
    /// Deletes the current user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteUserAsync(int id)
    {
        return await _apiService.DeleteAsync($"{BaseEndpoint}/{id}");
    }

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <param name="changePasswordDto">The change password DTO.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ChangePasswordAsync(UserChangePasswordDto changePasswordDto)
    {
        var (success, _) = await _apiService.PostAsync<UserChangePasswordDto, object>("api/auth/change-password", changePasswordDto);
        return success;
    }
}