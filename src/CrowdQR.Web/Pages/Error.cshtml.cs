using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for the error page.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ErrorModel"/> class.
/// </remarks>
/// <param name="logger">The logger for the error page.</param>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
    /// <summary>
    /// Gets or sets the request ID for the current error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether to show the request ID on the error page.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger = logger;

    /// <summary>
    /// Handles GET requests to the error page.
    /// </summary>
    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}