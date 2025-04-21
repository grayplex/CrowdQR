using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing song requests through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the RequestService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class RequestService(ApiService apiService, ILogger<RequestService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<RequestService> _logger = logger;
    private const string BaseEndpoint = "api/request";

    /// <summary>
    /// Gets all requests.
    /// </summary>
    /// <returns>A list of requests.</returns>
    public async Task<List<RequestDto>> GetRequestsAsync()
    {
        var requests = await _apiService.GetAsync<List<RequestDto>>(BaseEndpoint);
        return requests ?? [];
    }

    /// <summary>
    /// Gets a request by ID.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <returns>The request, or null if not found.</returns>
    public async Task<RequestDto?> GetRequestByIdAsync(int id)
    {
        return await _apiService.GetAsync<RequestDto>($"{BaseEndpoint}/{id}");
    }

    /// <summary>
    /// Gets all requests for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of requests for the event.</returns>
    public async Task<List<RequestDto>> GetRequestsByEventAsync(int eventId)
    {
        var requests = await _apiService.GetAsync<List<RequestDto>>($"{BaseEndpoint}/event/{eventId}");
        return requests ?? [];
    }

    /// <summary>
    /// Creates a new request.
    /// </summary>
    /// <param name="requestDto">The request data.</param>
    /// <returns>The created request and success flag.</returns>
    public async Task<(bool Success, RequestDto? Request)> CreateRequestAsync(RequestCreateDto requestDto)
    {
        return await _apiService.PostAsync<RequestCreateDto, RequestDto>(BaseEndpoint, requestDto);
    }

    /// <summary>
    /// Updates the status of a request.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <param name="statusDto">The new status data.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> UpdateRequestStatusAsync(int id, RequestStatusUpdateDto statusDto)
    {
        return await _apiService.PutAsync($"{BaseEndpoint}/{id}/status", statusDto);
    }

    /// <summary>
    /// Deletes a request.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteRequestAsync(int id)
    {
        return await _apiService.DeleteAsync($"{BaseEndpoint}/{id}");
    }
}