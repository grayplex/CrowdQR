namespace CrowdQR.Shared.Models.Enums;

/// <summary>
/// Represents the role of a user in the system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular audience member who can submit and vote on requests.
    /// </summary>
    Audience = 0,

    /// <summary>
    /// DJ who can manage events and approve/reject requests.
    /// </summary>
    DJ = 1
}