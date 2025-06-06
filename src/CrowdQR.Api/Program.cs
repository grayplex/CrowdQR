using CrowdQR.Api.Data;
using CrowdQR.Api.Middleware;
using CrowdQR.Api.Hubs;
using CrowdQR.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables configuration source
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddDbContext<CrowdQRContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ??
        BuildConnectionString(builder.Configuration);

    options.UseNpgsql(connectionString);
});

// Add controllers and other services
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});
builder.Services.AddScoped<IHubNotificationService, HubNotificationService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add JWT authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSecret = builder.Configuration["JWT_SECRET"] ??
                   builder.Configuration["Jwt:Secret"] ??
                   "test_jwt_secret_key_that_is_long_enough_for_testing_requirements_12345";

    // Ensure the secret is at least 32 characters (256 bits)
    if (jwtSecret.Length < 32)
    {
        throw new InvalidOperationException($"JWT secret must be at least 32 characters long. Current length: {jwtSecret.Length}");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CrowdQR.Web",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Enable debugging for JWT events
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {Exception}", context.Exception);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated successfully for user: {Name}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT token received: {Token}",
                context.Token?.Length > 10 ? context.Token[..10] + "..." : "None");
            return Task.CompletedTask;
        }
    };
});

// Add authorization services
builder.Services.AddAuthorization();

// Register auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("Default") ??
            BuildConnectionString(builder.Configuration),
        name: "postgresql",
        tags: ["database", "postgresql"])
    .AddCheck("self", () => HealthCheckResult.Healthy("Api is running"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        builder =>
        {
            builder.SetIsOriginAllowed(_ => true)
                    /*.WithOrigins(
                    "https://localhost:7159",
                    "http://localhost:5139")*/
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
        });
});

var app = builder.Build();

// Check for --wait-for-db argument - doing this AFTER app.Build() to avoid BuildServiceProvider issue
if (args.Contains("--wait-for-db"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();

    // Try to connect to the database
    bool connected = false;
    int retries = 0;
    const int maxRetries = 10;

    while (!connected && retries < maxRetries)
    {
        try
        {
            Console.WriteLine("Attempting to connect to the database...");
            connected = dbContext.Database.CanConnect();
            if (connected)
            {
                Console.WriteLine("Successfully connected to the database.");
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection failed: {ex.Message}");
        }

        retries++;
        await Task.Delay(1000);
    }

    Environment.Exit(1);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations and seed data in development environment
if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("Development"))
{
    using var migrationScope = app.Services.CreateScope();
    var services = migrationScope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CrowdQRContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Checking database connection and applying migrations...");
        context.Database.Migrate(); // Apply migrations
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw; // Re-throw to prevent startup with broken database
    }
}
else
{
    // For testing environment, just ensure the database is created
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Testing environment detected - ensuring in-memory database is created");
    await context.Database.EnsureCreatedAsync();
}

{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var jwtSecret = builder.Configuration["JWT_SECRET"] ??
                    builder.Configuration["Jwt:Secret"] ??
                    "test_jwt_secret_key_that_is_long_enough_for_testing_requirements_12345";
    logger.LogInformation("Program JWT Config - Secret: {SecretLength} chars, Issuer: {Issuer}, Audience: {Audience}",
        jwtSecret.Length,
        builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
        builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "CrowdQR.Web");
}


app.UseExceptionHandling(); // Custom middleware for global exception handling
app.UseCors("AllowWebApp"); // Enable CORS for the web app

app.UseRouting(); // Must be before auth middleware
app.UseAuthentication(); // Enable authentication middleware, must come before authorization
app.UseAuthorization(); // Enable authorization middleware

app.UseAuthorizationLogging(); // Custom middleware for logging authorization events
app.UseDjRoleValidation(); // Custom middleware for DJ role validation

// app.UseHttpsRedirection(); // Redirect HTTP to HTTPS // Disabled while in development mode
app.MapHub<CrowdQRHub>("/hubs/crowdqr"); // Map SignalR hub
app.MapControllers(); // Map controllers to routes

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

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run(); 

// Helper method to build connection string from individual environment variables
static string BuildConnectionString(IConfiguration configuration)
{
    var host = configuration["DB_HOST"] ?? "localhost";
    var port = configuration["DB_PORT"] ?? "5432";
    var database = configuration["DB_NAME"] ?? "crowdqr";
    var username = configuration["DB_USER"] ?? "postgres";
    var password = configuration["DB_PASSWORD"] ?? "postgres";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

namespace CrowdQR.Api
{
    /// <summary>
    /// Program class for integration tests.
    /// </summary>
    public partial class Program { }
}