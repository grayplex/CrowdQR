namespace CrowdQR.Api.Services;

/// <summary>
/// Service for sending emails.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EmailService"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="configuration">The configuration.</param>
public class EmailService(ILogger<EmailService> logger, IConfiguration configuration) : IEmailService
{
    private readonly ILogger<EmailService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc />
    public Task<bool> SendVerificationEmailAsync(string email, string username, string token)
    {
        // This is a placeholder implementation that logs instead of actually sending an email
        // Replace with actual email sending logic in production
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5000";
        var verificationUrl = $"{baseUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        // Sanitize user-provided inputs
        var sanitizedEmail = email.Replace("\r", "").Replace("\n", "");
        var sanitizedUsername = username.Replace("\r", "").Replace("\n", "");
        var sanitizedToken = token.Replace("\r", "").Replace("\n", "");

        _logger.LogInformation("----- VERIFICATION EMAIL -----");
        _logger.LogInformation("To: {Email}", sanitizedEmail);
        _logger.LogInformation("Subject: Verify your CrowdQR DJ account");
        _logger.LogInformation("Body:");
        _logger.LogInformation("Hello {Username},", sanitizedUsername);
        _logger.LogInformation("Thank you for registering as a DJ on CrowdQR.");
        _logger.LogInformation("Please verify your email by clicking the link below:");
        _logger.LogInformation("{Url}", verificationUrl);
        _logger.LogInformation("Or use the following verification code: {Token}", sanitizedToken);
        _logger.LogInformation("This link will expire in 24 hours.");
        _logger.LogInformation("If you did not create this account, please ignore this email.");
        _logger.LogInformation("Thanks,");
        _logger.LogInformation("The CrowdQR Team");
        _logger.LogInformation("-----------------------------");

        // In development, let's write the verification URL to the console
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine("✅ VERIFICATION LINK: " + verificationUrl);
        Console.WriteLine();
        Console.ResetColor();

        // Simulate successful email sending
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SendPasswordResetEmailAsync(string email, string username, string token)
    {
        // This is a placeholder implementation that logs instead of actually sending an email
        // Replace with actual email sending logic in production
        // Sanitize user-provided inputs
        var sanitizedEmail = email.Replace("\r", "").Replace("\n", "");
        var sanitizedUsername = username.Replace("\r", "").Replace("\n", "");
        var sanitizedToken = token.Replace("\r", "").Replace("\n", "");

        _logger.LogInformation(
            "PASSWORD RESET EMAIL: To: {Email}, For: {Username}, Token: {Token}",
            sanitizedEmail, sanitizedUsername, sanitizedToken);

        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5000";
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        _logger.LogInformation("Password Reset URL: {Url}", resetUrl);

        // Simulate successful email sending
        return Task.FromResult(true);
    }
}