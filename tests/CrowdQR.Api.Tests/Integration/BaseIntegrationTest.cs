using System.Text.Json;
using CrowdQR.Api.Data;
using CrowdQR.Api.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Base class for integration tests with common setup and utilities.
/// </summary>
public abstract class BaseIntegrationTest : IDisposable
{
    /// <summary>
    /// Factory for creating a test server and client for integration tests.
    /// </summary>
    protected readonly WebApplicationFactory<CrowdQR.Api.Program> Factory;

    /// <summary>
    /// The HTTP client used for sending requests to the test server.
    /// </summary>
    protected readonly HttpClient Client;

    /// <summary>
    /// The name of the in-memory database used for testing.
    /// </summary>
    protected string DatabaseName;

    /// <summary>
    /// JSON serializer options configured for camel case property naming and case-insensitive property names.
    /// </summary>
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseIntegrationTest"/> class.
    /// </summary>
    protected BaseIntegrationTest()
    {
        DatabaseName = $"TestDb_{GetType().Name}_{Guid.NewGuid()}";

        Factory = new WebApplicationFactory<CrowdQR.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove ALL Entity Framework related services
                    var descriptorsToRemove = new List<ServiceDescriptor>();

                    foreach (var service in services)
                    {
                        if (service.ServiceType.Namespace != null &&
                            (service.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                             service.ServiceType.Namespace.StartsWith("Npgsql.EntityFrameworkCore") ||
                             service.ServiceType == typeof(CrowdQRContext) ||
                             service.ServiceType == typeof(DbContextOptions<CrowdQRContext>) ||
                             service.ServiceType == typeof(DbContextOptions)))
                        {
                            descriptorsToRemove.Add(service);
                        }
                    }

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database
                    services.AddDbContext<CrowdQRContext>(options =>
                    {
                        options.UseInMemoryDatabase(DatabaseName);
                        options.EnableSensitiveDataLogging();
                    });

                    // Configure test authentication if needed
                    ConfigureTestAuthentication(services);
                });
            });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Override this method to configure test authentication for specific test classes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    protected virtual void ConfigureTestAuthentication(IServiceCollection services)
    {
        // Default: no special authentication configuration
        // Override in derived classes if needed
    }

    /// <summary>
    /// Clears all data from the in-memory database.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();

        // Remove all entities in dependency order (most dependent first)
        if (context.Votes.Any()) context.Votes.RemoveRange(context.Votes);
        if (context.Sessions.Any()) context.Sessions.RemoveRange(context.Sessions);
        if (context.Requests.Any()) context.Requests.RemoveRange(context.Requests);
        if (context.Events.Any()) context.Events.RemoveRange(context.Events);
        if (context.Users.Any()) context.Users.RemoveRange(context.Users);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    /// <param name="seedAction">Action to perform seeding.</param>
    protected async Task SeedDatabaseAsync(Action<CrowdQRContext> seedAction)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();

        seedAction(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the database with standard test data.
    /// </summary>
    protected async Task SeedStandardTestDataAsync()
    {
        await SeedDatabaseAsync(context =>
        {
            TestDbContextFactory.SeedTestData(context);
        });
    }

    /// <summary>
    /// Gets a fresh database context for direct database operations.
    /// </summary>
    /// <returns>A new database context instance.</returns>
    protected CrowdQRContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
    }

    /// <summary>
    /// Gets a user from the database by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    protected async Task<CrowdQR.Api.Models.User?> GetUserFromDatabaseAsync(string username)
    {
        using var context = GetDbContext();
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Verifies that a user exists in the database.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="because">The reason for the assertion.</param>
    protected async Task VerifyUserInDatabaseAsync(string username, string because)
    {
        var user = await GetUserFromDatabaseAsync(username);
        user.Should().NotBeNull(because);
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    public virtual void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}