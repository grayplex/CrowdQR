using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Pages.Admin
{
    /// <summary>
    /// Represents the settings page for users to manage their account settings.
    /// </summary>
    public class SettingsModel : PageModel
    {
        // Theme
        /// <summary>
        /// Gets or sets a value indicating whether the dark theme is enabled.
        /// </summary>
        [BindProperty]
        public bool IsDarkTheme { get; set; }

        // Profile Information
        /// <summary>
        /// Gets or sets the display name of the user.
        /// </summary>
        [BindProperty]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email of the user.
        /// </summary>
        [BindProperty]
        [EmailAddress]
        public string? Email { get; set; }

        // Password Change
        /// <summary>
        /// Gets or sets the current password of the user for verification.
        /// </summary>
        [BindProperty]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// Gets or sets the new password for the user.
        /// </summary>
        [BindProperty]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        /// <summary>
        /// Gets or sets the confirmation of the new password to ensure it matches.
        /// </summary>
        [BindProperty]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// Gets or sets the action to perform when the form is submitted (e.g., logout, delete account, change password).
        /// </summary>
        [BindProperty]
        public string? Action { get; set; }

        /// <summary>
        /// Initializes the settings page model and checks the user's theme preference from cookies.
        /// </summary>
        public void OnGet()
        {
            var themeCookie = Request.Cookies["theme"];
            IsDarkTheme = themeCookie == "dark";
        }

        /// <summary>
        /// Handles the form submission for various actions such as logout, delete account, or change password.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync()
        {
            // Handle theme switching
            if (Request.Form.ContainsKey("IsDarkTheme"))
            {
                Response.Cookies.Append("theme", IsDarkTheme ? "dark" : "light", new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false // Allow JavaScript to read this cookie for instant theme switching
                });
                return RedirectToPage();
            }

            // Handle other actions
            if (Action == "logout")
            {
                if (HttpContext.RequestServices.GetService(typeof(Services.AuthenticationService)) is Services.AuthenticationService authService)
                {
                    await authService.LogoutAsync();
                }
                return RedirectToPage("/Login", new { area = "" });
            }
            else if (Action == "delete")
            {
                if (HttpContext.RequestServices.GetService(typeof(Services.SessionManager)) is Services.SessionManager sessionManager && 
                    HttpContext.RequestServices.GetService(typeof(Services.UserService)) is Services.UserService userService && 
                    HttpContext.RequestServices.GetService(typeof(Services.AuthenticationService)) is Services.AuthenticationService authService)
                {
                    var userId = sessionManager.GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        await userService.DeleteUserAsync(userId.Value);
                        await authService.LogoutAsync();
                    }
                }
                return RedirectToPage("/Register", new { area = "" });
            }
            else if (!string.IsNullOrEmpty(CurrentPassword) && !string.IsNullOrEmpty(NewPassword) && !string.IsNullOrEmpty(ConfirmPassword))
            {
                if (HttpContext.RequestServices.GetService(typeof(Services.UserService)) is Services.UserService userService)
                {
                    var changePasswordDto = new ChangePasswordDto
                    {
                        CurrentPassword = CurrentPassword!,
                        NewPassword = NewPassword!,
                        ConfirmPassword = ConfirmPassword!
                    };
                    var success = await userService.ChangePasswordAsync(changePasswordDto);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Password changed successfully.";
                        return RedirectToPage();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Failed to change password. Please check your current password.");
                    }
                }
            }

            return Page();
        }
    }
} 