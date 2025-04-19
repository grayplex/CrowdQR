using CrowdQR.Api.Data;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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