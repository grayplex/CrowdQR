using CrowdQR.Web.Services;
using CrowdQR.Web.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Core;
using HealthChecks.UI;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure for reverse proxy (Traefik)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add API client configuration with detailed logging
builder.Services.AddHttpClientLogging();

// Add API client configuration
builder.Services.AddHttpClient("CrowdQRApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "CrowdQR.Web");
});

// Add Authentication with dual schemes
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "WebAppCookie";
    options.DefaultSignInScheme = "WebAppCookie";
})
.AddCookie("WebAppCookie", options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    // Configure for reverse proxy
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddCookie("AudienceCookie", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add Authorization
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DjOnly", policy =>
        policy.RequireAssertion(context =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return context.User.IsInRole("DJ");
            }
            return false;
        }))
    .AddPolicy("AudienceAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.IsAuthenticated == true ||
            (context.Resource is HttpContext httpContext &&
            httpContext.Session.GetString("AudienceId") != null)));

// Add API services
builder.Services.AddScoped<ApiService>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient("CrowdQRApi");
    var logger = sp.GetRequiredService<ILogger<ApiService>>();
    var config = sp.GetRequiredService<IConfiguration>();
    var tokenRefresher = sp.GetRequiredService<ApiTokenRefresher>();
    return new ApiService(client, logger, config, tokenRefresher);
});

// Add all API service clients
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<VoteService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AuthenticationService>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient("CrowdQRApi");
    var logger = sp.GetRequiredService<ILogger<AuthenticationService>>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new AuthenticationService(client, logger, httpContextAccessor, config);
});

// Add token refresher
builder.Services.AddScoped<ApiTokenRefresher>();

// Add session state
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add session manager for audience members
builder.Services.AddScoped<SessionManager>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Web app is running"))
    .AddUrlGroup(
        new Uri($"{builder.Configuration["ApiSettings:BaseUrl"]}/health"),
        name: "api-health",
        tags: ["api", "external"]);


// Add services to the container
builder.Services.AddRazorPages(options =>
{
    // Require DJ role for admin pages
    options.Conventions.AuthorizeFolder("/Admin", "DjOnly");
});
builder.Services.AddSignalR();

// Setup detailed logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Disable HTTP to HTTPS redirection when running behind a reverse proxy like Traefik
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithRedirects("/AccessDenied?code={0}");
app.MapRazorPages();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Theme endpoint
app.MapPost("/api/theme", (HttpContext context, JsonDocument body) =>
{
    var isDarkTheme = body.RootElement.GetProperty("isDarkTheme").GetBoolean();
    context.Response.Cookies.Append("theme", isDarkTheme ? "dark" : "light", new CookieOptions 
    { 
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = false // Allow JavaScript to read this cookie
    });
    return Results.Ok();
});

app.Run();
