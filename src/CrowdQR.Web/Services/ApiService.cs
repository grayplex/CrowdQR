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
            // Ensure token is attached
            AttachToken();

            _logger.LogInformation("Making GET request to {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response from {Endpoint}: Status={StatusCode}, Content={Content}",
                endpoint, response.StatusCode, responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response from {Endpoint}: {Content}", endpoint, responseContent);
                    return default;
                }
            }

            _logger.LogWarning("API request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="defaultErrorMessage">The default error message to use if the API doesn't provide one.</param>
    /// <returns>The deserialized response.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
    public async Task<T?> GetWithErrorHandlingAsync<T>(string endpoint, string defaultErrorMessage = "Failed to retrieve data")
    {
        try
        {
            // Ensure token is attached
            AttachToken();

            _logger.LogInformation("Making GET request to {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response from {Endpoint}: {Content}", endpoint, responseContent);
                    throw new ApiException("Failed to parse API response", ex);
                }
            }

            _logger.LogWarning("API request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="endpoint">The API endpoint.</param>
    /// <param name="data">The request body.</param>
    /// <param name="defaultErrorMessage">The default error message to use if the API doesn't provide one.</param>
    /// <returns>True if the request was successful, false otherwise.</returns>
    /// <exception cref="ApiException">Thrown when the API request fails.</exception>
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
            // Ensure token is attached
            AttachToken();

            // Log the request details
            var requestJson = JsonSerializer.Serialize(data, _jsonOptions);
            _logger.LogInformation("Making POST request to {Endpoint} with data: {Data}",
                endpoint, requestJson);

            // Send the request
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            // Read and log the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response from {Endpoint}: Status={StatusCode}, Content={Content}",
                endpoint, response.StatusCode, responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                    return (true, result);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing response from {Endpoint}: {Content}", endpoint, responseContent);
                    return (false, default);
                }
            }

            _logger.LogWarning("API POST request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
            // Ensure token is attached
            AttachToken();

            // Log the request details
            var requestJson = JsonSerializer.Serialize(data, _jsonOptions);
            _logger.LogInformation("Making PUT request to {Endpoint} with data: {Data}",
                endpoint, requestJson);

            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);

            // Read and log the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response from {Endpoint}: Status={StatusCode}, Content={Content}",
                endpoint, response.StatusCode, responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API PUT request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
            // Ensure token is attached
            AttachToken();

            _logger.LogInformation("Making PUT request to {Endpoint} with no body", endpoint);

            var response = await _httpClient.PutAsync(endpoint, null);

            // Read and log the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response from {Endpoint}: Status={StatusCode}, Content={Content}",
                endpoint, response.StatusCode, responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API PUT request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
            // Ensure token is attached
            AttachToken();

            _logger.LogInformation("Making DELETE request to {Endpoint}", endpoint);

            var response = await _httpClient.DeleteAsync(endpoint);

            // Read and log the response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response from {Endpoint}: Status={StatusCode}, Content={Content}",
                endpoint, response.StatusCode, responseContent.Length > 100 ? responseContent[..100] + "..." : responseContent);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("API DELETE request failed: {Endpoint} - {StatusCode} - {Content}",
                endpoint, response.StatusCode, responseContent);

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
            // Ensure token is attached
            AttachToken();

            // Log the request in detail
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            _logger.LogInformation("DEBUG POST Request to {Endpoint}:", endpoint);
            _logger.LogInformation("Request Content: {Content}", jsonContent);

            // Log token status
            var hasAuth = _httpClient.DefaultRequestHeaders.Authorization != null;
            _logger.LogInformation("Authorization header present: {HasAuth}", hasAuth);
            if (hasAuth)
            {
                _logger.LogInformation("Authorization header scheme: {Scheme}",
                    _httpClient.DefaultRequestHeaders.Authorization?.Scheme ?? "No Authorization Header");

                var token = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;

                if (!string.IsNullOrEmpty(token) && token.Length > 10)
                {
                    _logger.LogInformation("Token prefix: {TokenPrefix}...", token[..10]);
                }
            }

            // Check all request headers
            _logger.LogInformation("All Request Headers:");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }

            // Create explicit request message for more control
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // Send the request
            var response = await _httpClient.SendAsync(request);

            // Log the response in detail
            var statusCode = (int)response.StatusCode;
            _logger.LogInformation("DEBUG Response Status: {StatusCode} {ReasonPhrase}",
                statusCode, response.ReasonPhrase);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("DEBUG Response Content: {Content}", responseContent);

            // Log response headers
            _logger.LogInformation("Response Headers:");
            foreach (var header in response.Headers)
            {
                _logger.LogInformation("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                    return (true, result);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "DEBUG Error deserializing response: {Content}", responseContent);
                    return (false, default);
                }
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