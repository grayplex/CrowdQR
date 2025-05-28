using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for accessing dashboard-related data through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the DashboardService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class DashboardService(ApiService apiService, ILogger<DashboardService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<DashboardService> _logger = logger;
    private const string BaseEndpoint = "api/dashboard";

    /// <summary>
    /// Gets a summary of an event for the DJ dashboard.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event summary, or null if not found.</returns>
    public async Task<DashboardEventSummaryDto?> GetEventSummaryAsync(int eventId)
    {
        return await _apiService.GetAsync<DashboardEventSummaryDto>($"{BaseEndpoint}/event/{eventId}/summary");
    }

    /// <summary>
    /// Gets the top requests for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="status">The request status (0 = Pending, 1 = Approved, 2 = Rejected).</param>
    /// <param name="count">The number of requests to return.</param>
    /// <returns>A list of the top requests.</returns>
    public async Task<List<DashboardTopRequestDto>> GetTopRequestsAsync(int eventId, int status = 0, int count = 10)
    {
        var requests = await _apiService.GetAsync<List<DashboardTopRequestDto>>(
            $"{BaseEndpoint}/event/{eventId}/top-requests?status={status}&count={count}");
        return requests ?? [];
    }

    /// <summary>
    /// Gets statistics for all events of a DJ.
    /// </summary>
    /// <param name="djUserId">The DJ user ID.</param>
    /// <returns>A list of event statistics.</returns>
    public async Task<List<DashboardDjEventStatDto>> GetDjEventStatsAsync(int djUserId)
    {
        var stats = await _apiService.GetAsync<List<DashboardDjEventStatDto>>($"{BaseEndpoint}/dj/{djUserId}/event-stats");
        return stats ?? [];
    }
}