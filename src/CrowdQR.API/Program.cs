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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

// Helper method to build connection string from individual environment variables
string BuildConnectionString(IConfiguration configuration)
{
    var host = configuration["DB_HOST"] ?? "localhost";
    var port = configuration["DB_PORT"] ?? "5432";
    var database = configuration["DB_NAME"] ?? "crowdqr";
    var username = configuration["DB_USER"] ?? "postgres";
    var password = configuration["DB_PASSWORD"] ?? "postgres";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}