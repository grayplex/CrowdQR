using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for the privacy policy page.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PrivacyModel"/> class.
/// </remarks>
/// <param name="logger">The logger for the privacy page.</param>
public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
{
    private readonly ILogger<PrivacyModel> _logger = logger;

    /// <summary>
    /// Handles GET requests to the privacy page.
    /// </summary>
    public void OnGet()
    {
        // This page currently doesn't have any specific logic
    }
}