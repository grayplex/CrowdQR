using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IndexModel"/> class.
/// </remarks>
/// <param name="logger">The logger for the home page.</param>
public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    /// <summary>
    /// Handles GET requests to the home page.
    /// </summary>
    public void OnGet()
    {
        // This page currently doesn't have any specific logic
    }
}