using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for generating reports through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ReportService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class ReportService(ApiService apiService, ILogger<ReportService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<ReportService> _logger = logger;
    private const string BaseEndpoint = "api/reports";

    /// <summary>
    /// Generates an event performance report.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>The event performance report, or null if not found.</returns>
    public async Task<EventPerformanceReportDto?> GetEventPerformanceReportAsync(int eventId)
    {
        return await _apiService.GetAsync<EventPerformanceReportDto>($"{BaseEndpoint}/event-performance/{eventId}");
    }

    /// <summary>
    /// Generates a DJ analytics report.
    /// </summary>
    /// <returns>The DJ analytics report, or null if not found.</returns>
    public async Task<DjAnalyticsReportDto?> GetDjAnalyticsReportAsync()
    {
        return await _apiService.GetAsync<DjAnalyticsReportDto>($"{BaseEndpoint}/dj-analytics");
    }
}