using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing events through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the EventService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class EventService(ApiService apiService, ILogger<EventService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<EventService> _logger = logger;
    private const string BaseEndpoint = "api/event";

    /// <summary>
    /// Gets all events.
    /// </summary>
    /// <returns>A list of events.</returns>
    public async Task<List<EventDto>> GetEventsAsync()
    {
        var events = await _apiService.GetAsync<List<EventDto>>(BaseEndpoint);
        return events ?? [];
    }

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <returns>The event, or null if not found.</returns>
    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        return await _apiService.GetAsync<EventDto>($"{BaseEndpoint}/{id}");
    }

    /// <summary>
    /// Gets an event by slug.
    /// </summary>
    /// <param name="slug">The event slug.</param>
    /// <returns>The event, or null if not found.</returns>
    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        return await _apiService.GetAsync<EventDto>($"{BaseEndpoint}/slug/{slug}");
    }

    /// <summary>
    /// Gets all events for a DJ.
    /// </summary>
    /// <param name="djUserId">The DJ user ID.</param>
    /// <returns>A list of events for the DJ.</returns>
    public async Task<List<EventDto>> GetEventsByDjAsync(int djUserId)
    {
        var events = await _apiService.GetAsync<List<EventDto>>($"{BaseEndpoint}/dj/{djUserId}");
        return events ?? [];
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventDto">The event data.</param>
    /// <returns>The created event and success flag.</returns>
    public async Task<(bool Success, EventDto? Event)> CreateEventAsync(EventCreateDto eventDto)
    {
        try
        {
            _logger.LogInformation("Creating event: {EventName} with slug {Slug} for DJ {DjUserId}",
                eventDto.Name, eventDto.Slug, eventDto.DjUserId);

            var (success, response) = await _apiService.PostAsync<EventCreateDto, EventDto>(BaseEndpoint, eventDto);

            if (success && response != null)
            {
                return (true, response);
            }

            _logger.LogWarning("Failed to create event {EventName}", eventDto.Name);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event {EventName}", eventDto.Name);
            return (false, null);
        }
    }

    /// <summary>
    /// Updates an event.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="eventDto">The updated event data.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> UpdateEventAsync(int id, EventUpdateDto eventDto)
    {
        return await _apiService.PutAsync($"{BaseEndpoint}/{id}", eventDto);
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteEventAsync(int id)
    {
        return await _apiService.DeleteAsync($"{BaseEndpoint}/{id}");
    }
}