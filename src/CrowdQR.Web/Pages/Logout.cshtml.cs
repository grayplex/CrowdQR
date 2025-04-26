using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for user logout.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LogoutModel"/> class.
/// </remarks>
/// <param name="authService">The authentication service.</param>
/// <param name="logger">The logger.</param>
public class LogoutModel(AuthenticationService authService, ILogger<LogoutModel> logger) : PageModel
{
    private readonly AuthenticationService _authService = authService;
    private readonly ILogger<LogoutModel> _logger = logger;

    /// <summary>
    /// Handles POST requests to log out.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await _authService.LogoutAsync();
        return RedirectToPage("/Index");
    }
}