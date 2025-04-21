using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for displaying QR codes to join events.
/// </summary>
public class QRModel : PageModel
{
    /// <summary>
    /// The slug/code for the event.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The full URL for the event that will be encoded in the QR code.
    /// </summary>
    public string EventUrl { get; set; } = string.Empty;

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public void OnGet()
    {
        // In a real implementation, we would fetch the event from the API
        // For now, just use dummy data
        EventName = Slug == "demo" ? "Saturday Night Fever" : $"Event {Slug}";

        // Create the absolute URL for the event
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        EventUrl = $"{baseUrl}/event/{Slug}";
    }
}