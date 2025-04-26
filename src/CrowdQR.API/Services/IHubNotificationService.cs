namespace CrowdQR.Api.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR.
/// </summary>
public interface IHubNotificationService
{
    /// <summary>
    /// Notifies clients that a new request has been added to an event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="requestId">The ID of the new request.</param>
    /// <param name="requesterName"> The name of the user who added the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyRequestAdded(int eventId, int requestId, string requesterName);

    /// <summary>
    /// Notifies clients that a request's status has been updated.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="requestId">The ID of the updated request.</param>
    /// <param name="newStatus">The new status of the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyRequestStatusUpdated(int eventId, int requestId, string newStatus);

    /// <summary>
    /// Notifies clients that a new vote has been added to a request.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="requestId">The ID of the request that received a vote.</param>
    /// <param name="voteCount">The new vote count.</param>
    /// <param name="userId">The ID of the user who voted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyVoteAdded(int eventId, int requestId, int voteCount, int userId);

    /// <summary>
    /// Notifies clients that a vote has been removed from a request.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="requestId">The ID of the request that lost a vote.</param>
    /// <param name="voteCount">The new vote count.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyVoteRemoved(int eventId, int requestId, int voteCount);

    /// <summary>
    /// Notifies clients that a user has joined an event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="username">The username of the user who joined.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyUserJoinedEvent(int eventId, string username);
}