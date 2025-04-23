using CrowdQR.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CrowdQR.Api.Services;

/// <summary>
/// Service implementation for sending real-time notifications via SignalR.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HubNotificationService"/> class.
/// </remarks>
/// <param name="hubContext">The SignalR hub context.</param>
/// <param name="logger">The logger.</param>
public class HubNotificationService(IHubContext<CrowdQRHub> hubContext, ILogger<HubNotificationService> logger) : IHubNotificationService
{
    private readonly IHubContext<CrowdQRHub> _hubContext = hubContext;
    private readonly ILogger<HubNotificationService> _logger = logger;

    /// <inheritdoc/>
    public async Task NotifyRequestAdded(int eventId, int requestId, string requesterName)
    {
        _logger.LogInformation("Broadcasting RequestAdded for event {EventId}, request {RequestId}", eventId, requestId);
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("requestAdded", new { eventId, requestId, requesterName });
    }

    /// <inheritdoc/>
    public async Task NotifyRequestStatusUpdated(int eventId, int requestId, string newStatus)
    {
        _logger.LogInformation("Broadcasting RequestStatusUpdated for event {EventId}, request {RequestId}, status {Status}",
            eventId, requestId, newStatus);
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("requestStatusUpdated", new { eventId, requestId, status = newStatus });
    }

    /// <inheritdoc/>
    public async Task NotifyVoteAdded(int eventId, int requestId, int voteCount, int userId)
    {
        _logger.LogInformation("Broadcasting VoteAdded for event {EventId}, request {RequestId}, count {VoteCount}",
            eventId, requestId, voteCount);
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("voteAdded", new { eventId, requestId, voteCount, userId });
    }

    /// <inheritdoc/>
    public async Task NotifyVoteRemoved(int eventId, int requestId, int voteCount)
    {
        _logger.LogInformation("Broadcasting VoteRemoved for event {EventId}, request {RequestId}, count {VoteCount}",
            eventId, requestId, voteCount);
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("voteRemoved", new { eventId, requestId, voteCount });
    }

    /// <inheritdoc/>
    public async Task NotifyUserJoinedEvent(int eventId, string username)
    {
        _logger.LogInformation("Broadcasting UserJoinedEvent for event {EventId}, user {Username}", eventId, username);
        await _hubContext.Clients.Group($"event-{eventId}")
            .SendAsync("userJoinedEvent", new { eventId, username });
    }
}