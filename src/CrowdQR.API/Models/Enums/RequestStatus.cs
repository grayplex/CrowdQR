namespace CrowdQR.Api.Models.Enums;

/// <summary>
/// Represents the status of a song request.
/// </summary>
public enum RequestStatus
{
    /// <summary>
    /// Request is awaiting DJ review.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Request has been approved by the DJ.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Request has been rejected by the DJ.
    /// </summary>
    Rejected = 2
}