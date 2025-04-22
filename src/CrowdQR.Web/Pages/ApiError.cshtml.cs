using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for displaying API error information.
/// </summary>
public class ApiErrorModel : PageModel
{
    /// <summary>
    /// The error message to display.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string ErrorMessage { get; set; } = "An error occurred while communicating with the server.";

    /// <summary>
    /// The URL to return to after error.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public void OnGet()
    {
        // If no return URL is specified, default to home page
        if (string.IsNullOrEmpty(ReturnUrl) || ReturnUrl == "/ApiError")
        {
            ReturnUrl = "/";
        }
    }
}