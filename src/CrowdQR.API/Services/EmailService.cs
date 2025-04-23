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
        _logger.LogInformation(
            "VERIFICATION EMAIL: To: {Email}, For: {Username}, Token: {Token}",
            email, username, token);

        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5000";
        var verificationUrl = $"{baseUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        _logger.LogInformation("Verification URL: {Url}", verificationUrl);

        // Simulate successful email sending
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SendPasswordResetEmailAsync(string email, string username, string token)
    {
        // This is a placeholder implementation that logs instead of actually sending an email
        // Replace with actual email sending logic in production
        _logger.LogInformation(
            "PASSWORD RESET EMAIL: To: {Email}, For: {Username}, Token: {Token}",
            email, username, token);

        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5000";
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        _logger.LogInformation("Password Reset URL: {Url}", resetUrl);

        // Simulate successful email sending
        return Task.FromResult(true);
    }
}