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

var builder = WebApplication.CreateBuilder(args);

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
})
.AddCookie("AudienceCookie", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
});

// Add Authorization
builder.Services.AddAuthorizationBuilder()
                        // Add Authorization
                        .AddPolicy("DjOnly", policy =>
        policy.RequireRole("DJ"))
                        // Add Authorization
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
});

// Add session manager for audience members
builder.Services.AddScoped<SessionManager>();


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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithRedirects("/AccessDenied?code={0}");
app.MapRazorPages();

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
