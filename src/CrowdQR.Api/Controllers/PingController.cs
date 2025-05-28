using Microsoft.AspNetCore.Mvc;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// Simple controller for API health checks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    /// <summary>
    /// Simple endpoint to verify API connectivity.
    /// </summary>
    /// <returns>A simple JSON response indicating the API is online.</returns>
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}