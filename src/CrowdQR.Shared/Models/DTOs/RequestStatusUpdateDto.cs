using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for updating a request's status.
/// </summary>
public class RequestStatusUpdateDto
{
    /// <summary>
    /// The new status for the request.
    /// </summary>
    [Required]
    public RequestStatus Status { get; set; }
}
