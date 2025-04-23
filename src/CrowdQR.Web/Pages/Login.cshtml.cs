using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for user login.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoginModel"/> class.
/// </remarks>
/// <param name="authService">The authentication service.</param>
/// <param name="logger">The logger.</param>
public class LoginModel(AuthenticationService authService, ILogger<LoginModel> logger) : PageModel
{
    private readonly AuthenticationService _authService = authService;
    private readonly ILogger<LoginModel> _logger = logger;

    /// <summary>
    /// Bound property for the login username or email.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Username or email is required")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Bound property for the login password.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Return URL after successful login.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public IActionResult OnGet()
    {
        // Redirect to home if already logged in
        if (_authService.IsLoggedIn())
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to log in.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var success = await _authService.LoginAsync(UsernameOrEmail, Password);

        if (!success)
        {
            ErrorMessage = "Invalid username/email or password. Please try again.";
            return Page();
        }

        // Redirect to return URL or home page
        return Redirect(!string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/");
    }
}