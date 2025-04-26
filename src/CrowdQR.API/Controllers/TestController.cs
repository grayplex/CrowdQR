using CrowdQR.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// Controller for testing SignalR functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestController"/> class.
/// </remarks>
/// <param name="hubService">The hub notification service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "DJ")]
public class TestController(IHubNotificationService hubService, ILogger<TestController> logger) : ControllerBase
{
    private readonly IHubNotificationService _hubService = hubService;
    private readonly ILogger<TestController> _logger = logger;

    /// <summary>
    /// Sends a test broadcast message to a specific event group.
    /// </summary>
    /// <param name="eventId">The event ID to broadcast to.</param>
    /// <returns>An action result indicating success or failure.</returns>
    [HttpGet("broadcast/{eventId}")]
    public async Task<IActionResult> Broadcast(int eventId)
    {
        try
        {
            await _hubService.NotifyRequestAdded(eventId, 999, "TestControllerUser");
            await _hubService.NotifyUserJoinedEvent(eventId, "TestUser");

            return Ok(new { message = $"Broadcast sent to event {eventId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast to event {EventId}", eventId);
            return StatusCode(500, new { error = "Failed to send broadcast" });
        }
    }
}