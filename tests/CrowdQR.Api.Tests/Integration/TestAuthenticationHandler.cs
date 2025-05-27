using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Test authentication handler for integration tests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestAuthenticationHandler"/> class.
/// </remarks>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
/// <param name="encoder">The URL encoder.</param>
public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Handles the authentication.
    /// </summary>
    /// <returns>The authentication result.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        if (authorizationHeader == null || !authorizationHeader.StartsWith("Test "))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid Authorization Header"));
        }

        var authValue = authorizationHeader["Test ".Length..];
        var claims = new List<Claim>();

        // Parse test authentication values
        switch (authValue)
        {
            case "user-1-dj":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "1"));
                claims.Add(new Claim(ClaimTypes.Role, "DJ"));
                claims.Add(new Claim(ClaimTypes.Name, "test_dj"));
                claims.Add(new Claim(ClaimTypes.Email, "dj@test.com"));
                claims.Add(new Claim("email_verified", "true"));
                break;
            case "user-2-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "2"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "audience1"));
                break;
            case "user-3-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "3"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "audience2"));
                break;
            case "user-999-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "999"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "nonexistent"));
                break;
            default:
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}