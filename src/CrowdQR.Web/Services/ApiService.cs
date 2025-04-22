using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for making API calls to the backend.
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the ApiService class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The configuration.</param>
    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Set the base address from configuration
        string apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
        _httpClient.BaseAddress = new Uri(apiBaseUrl);
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
}