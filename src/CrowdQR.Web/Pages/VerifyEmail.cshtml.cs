using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CrowdQR.Web.Services;
using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for email verification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VerifyEmailModel"/> class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class VerifyEmailModel(ApiService apiService, ILogger<VerifyEmailModel> logger) : PageModel
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<VerifyEmailModel> _logger = logger;

    /// <summary>
    /// Gets or sets the email address to verify.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the verification token.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the verification was successful.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the message to display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Token))
        {
            Message = "Invalid verification link. Please ensure you clicked the correct link or contact support.";
            return Page();
        }

        try
        {
            var verifyDto = new VerifyEmailDto
            {
                Email = Email,
                Token = Token
            };

            // FIX: Use a strongly typed DTO for the response
            var (success, response) = await _apiService.PostAsync<object, object>("api/auth/verify-email", verifyDto);

            /*IsVerified = response;
            Message = IsVerified
                ? "Your email has been verified successfully! You can now log in."
                : "Invalid or expired verification link. Please request a new verification link.";
            */
            return Page();
        }
        catch (ApiService.ApiException ex)
        {
            _logger.LogError(ex, "Error verifying email {Email}", Email);
            Message = "Error verifying email: " + ex.Message;
            return Page();
        }
    }
}