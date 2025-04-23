using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Utilities;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for making API calls to the backend.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ApiService class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="logger">The logger.</param>
/// <param name="configuration">The configuration.</param>
/// <param name="tokenRefresher"> The API token refresher.</param>
public class ApiService(
    HttpClient httpClient,
    ILogger<ApiService> logger, 
    IConfiguration configuration, 
    ApiTokenRefresher tokenRefresher)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ApiService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly ApiTokenRefresher _tokenRefresher = tokenRefresher;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Add before each HTTP request
    private void AttachToken()
    {
        _tokenRefresher.AttachTokenToClient(_httpClient);
    }

    /// <summary>
    /// Custom exception for API errors.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the ApiException class.
    /// </remarks>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public class ApiException(string message, Exception? innerException = null) : Exception(message, innerException)
    {
    }

    /// <summary>
    /// Makes a GET request to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <returns>The deserialized response, or default if the request failed.</returns>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            AttachToken();
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            _logger.LogWarning("API request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to {Endpoint}", endpoint);
            return default;
        }
    }

    /// <summary>
    /// Makes a GET request to the specified endpoint, with error handling
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="defaultErrorMessage"></param>
    /// <returns></returns>
    /// <exception cref="ApiException"></exception>
    public async Task<T?> GetWithErrorHandlingAsync<T>(string endpoint, string defaultErrorMessage = "Failed to retrieve data")
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            _logger.LogWarning("API request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API error: {response.StatusCode}. Details: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error in GET request to {Endpoint}", endpoint);
            throw new ApiException(defaultErrorMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making GET request to {Endpoint}", endpoint);
            throw new ApiException(defaultErrorMessage, ex);
        }
    }

    /// <summary>
    /// Makes a POST request to the specified endpoint with error handling.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="data"></param>
    /// <param name="defaultErrorMessage"></param>
    /// <returns></returns>
    /// <exception cref="ApiException"></exception>
    public async Task<bool> PostWithErrorHandlingAsync<TRequest>(string endpoint, TRequest data, string defaultErrorMessage = "Failed to save data")
    {
        try
        {
            var (success, _) = await PostAsync<TRequest, object>(endpoint, data);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API POST request failed: {Endpoint}", endpoint);
            throw new ApiException(defaultErrorMessage, ex);
        }
    }

    /// <summary>
    /// Makes a POST request to the specified endpoint.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The request body.</param>
    /// <returns>The deserialized response and success flag.</returns>
    public async Task<(bool Success, TResponse? Response)> PostAsync<TRequest, TResponse>(
        string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                return (true, result);
            }

            _logger.LogWarning("API POST request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            return (false, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making POST request to {Endpoint}", endpoint);
            return (false, default);
        }
    }

    /// <summary>
    /// Makes a PUT request to the specified endpoint.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The request body.</param>
    /// <returns>True if the request was successful, false otherwise.</returns>
    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API PUT request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making PUT request to {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// Makes a PUT request to the specified endpoint without a body.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <returns>True if the request was successful, false otherwise</returns>
    public async Task<bool> PutAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.PutAsync(endpoint, null);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API PUT request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making PUT request to {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// Makes a DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <returns>True if the request was successful, false otherwise.</returns>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API DELETE request failed: {Endpoint} - {StatusCode}",
                endpoint, response.StatusCode);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making DELETE request to {Endpoint}", endpoint);
            return false;
        }
    }

    private void LogApiError(string endpoint, Exception ex)
    {
        _logger.LogError(ex, "API request failed: {Endpoint} - {Message}", endpoint, ex.Message);
    }

    /// <summary>
    /// Handles API errors consistently.
    /// </summary>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <returns>A user-friendly error message.</returns>
    public string HandleApiError(string endpoint, Exception ex)
    {
        LogApiError(endpoint, ex);
        return ApiErrorHelper.GetUserFriendlyErrorMessage(ex);
    }

    /// <summary>
    /// Makes a debug POST request with detailed logging.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The request data.</param>
    /// <returns>The response and success flag.</returns>
    public async Task<(bool Success, TResponse? Response)> DebugPostAsync<TRequest, TResponse>(
        string endpoint, TRequest data)
    {
        try
        {
            AttachToken();

            // Log the request
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            _logger.LogInformation("DEBUG POST Request to {Endpoint}: {Content}", endpoint, jsonContent);

            // Log token status
            var hasAuth = _httpClient.DefaultRequestHeaders.Authorization != null;
            _logger.LogInformation("Authorization header present: {HasAuth}", hasAuth);

            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            // Log the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("DEBUG Response {StatusCode}: {Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                return (true, result);
            }

            return (false, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEBUG Error in POST request to {Endpoint}", endpoint);
            return (false, default);
        }
    }
}