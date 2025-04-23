using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using CrowdQR.Web.Services;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Web.Pages
{
    /// <summary>
    /// Page model for the user registration page
    /// </summary>
    public class RegisterModel : PageModel
    {
        private readonly ApiService _apiService;

        /// <summary>
        /// Initializes a new instance of the RegisterModel class
        /// </summary>
        /// <param name="apiService">The API service for making HTTP requests</param>
        public RegisterModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Gets or sets the username for registration
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the email address for registration
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the password for registration
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the password confirmation
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// Gets or sets whether the user is registering as a DJ
        /// </summary>
        [BindProperty]
        public bool IsDj { get; set; }

        /// <summary>
        /// Gets or sets any error message to display
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Handles GET requests to the registration page
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Handles POST requests for user registration
        /// </summary>
        /// <returns>The result of the registration attempt</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (IsDj)
                {
                    // DJ registration
                    var djRegisterRequest = new
                    {
                        Username = Username,
                        Email = Email,
                        Password = Password,
                        ConfirmPassword = ConfirmPassword
                    };

                    var (success, response) = await _apiService.PostAsync<object, object>("/api/auth/register", djRegisterRequest);
                    
                    if (success)
                    {
                        // Registration successful, redirect to login
                        return RedirectToPage("/Login", new { message = "Registration successful! Please check your email to verify your account." });
                    }
                    else
                    {
                        ErrorMessage = "Registration failed. Please try again.";
                        return Page();
                    }
                }
                else
                {
                    // Audience registration - use the user service directly
                    var userCreateRequest = new
                    {
                        Username = Username,
                        Role = UserRole.Audience
                    };

                    var (success, response) = await _apiService.PostAsync<object, object>("/api/user", userCreateRequest);
                    
                    if (success)
                    {
                        // Registration successful, redirect to login
                        return RedirectToPage("/Login", new { message = "Registration successful! You can now log in." });
                    }
                    else
                    {
                        ErrorMessage = "Registration failed. Please try again.";
                        return Page();
                    }
                }
            }
            catch
            {
                ErrorMessage = "An error occurred during registration. Please try again.";
                return Page();
            }
        }
    }
} 