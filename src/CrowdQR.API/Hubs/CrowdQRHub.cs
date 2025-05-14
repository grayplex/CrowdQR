using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace CrowdQR.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time updates in the CrowdQR application.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CrowdQRHub"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
public class CrowdQRHub(ILogger<CrowdQRHub> logger) : Hub
{
    private readonly ILogger<CrowdQRHub> _logger = logger;

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task that represents the asynchronous connection operation.</returns>
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        _logger.LogInformation(
            "Client connected: {ConnectionId}, IP: {IPAddress}, UserAgent: {UserAgent}",
            Context.ConnectionId,
            httpContext?.Connection?.RemoteIpAddress,
            httpContext?.Request.Headers.UserAgent);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, or null if the disconnection was graceful.</param>
    /// <returns>A task that represents the asynchronous disconnection operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}. Exception: {Exception}",
            Context.ConnectionId,
            exception?.Message ?? "None");

        // Broadcast userLeftEvent if the user was part of an event group
        var httpContext = Context.GetHttpContext();
        var eventId = httpContext?.Request.Query["eventId"].ToString();
        if (int.TryParse(eventId, out var parsedEventId))
        {
            var username = Context.User?.Identity?.Name ?? "Anonymous";
            await Clients.Group($"event-{parsedEventId}").SendAsync("userLeftEvent", new { eventId = parsedEventId, username });
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows a client to join a specific event group to receive updates for that event.
    /// </summary>
    /// <param name="eventId">The ID of the event to join.</param>
    /// <returns>A task that represents the asynchronous join operation.</returns>
    public async Task JoinEvent(int eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
        await Clients.Group($"event-{eventId}").SendAsync("userJoinedEvent", new { eventId });
        _logger.LogInformation("Client {ConnectionId} joined event {EventId}", Context.ConnectionId, eventId);
    }

    /// <summary>
    /// Allows a client to leave a specific event group.
    /// </summary>
    /// <param name="eventId">The ID of the event to leave.</param>
    /// <returns>A task that represents the asynchronous leave operation.</returns>
    public async Task LeaveEvent(int eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
        await Clients.Group($"event-{eventId}").SendAsync("userLeftEvent", new { eventId });
        _logger.LogInformation("Client {ConnectionId} left event {EventId}", Context.ConnectionId, eventId);
    }

    /// <summary>
    /// Simple ping method to check connection health
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task Ping() 
    {
        return Task.CompletedTask;
    }
}
