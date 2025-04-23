using CrowdQR.Api.Data;
using CrowdQR.Api.Middleware;
using CrowdQR.Api.Hubs;
using CrowdQR.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
builder.Services.AddSignalR();
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
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "temporaryCrowdQRSecretKey12345!@#$%")),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CrowdQR.Web",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization services
builder.Services.AddAuthorization();

// Register auth service
builder.Services.AddScoped<AuthService>();

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

    // Apply migrations and seed data in development environment
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CrowdQRContext>();
        DbSeeder.SeedAsync(context).Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.UseExceptionHandling();
app.UseAuthorizationLogging();
app.UseCors("AllowWebApp");
app.UseRouting();
app.UseAuthorization();
app.UseAuthentication();
app.MapHub<CrowdQRHub>("/hubs/crowdqr");

app.UseHttpsRedirection();
app.MapControllers();

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