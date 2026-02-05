# Technology Stack

**Analysis Date:** 2026-02-05

## Languages

**Primary:**
- C# - .NET 8.0 - Used for all API, Web, and Shared library code

## Runtime

**Environment:**
- .NET 8.0 (net8.0 target framework)

**Package Manager:**
- NuGet - Official .NET package manager
- Lockfile: Not applicable (uses .csproj package references)

## Frameworks

**Core:**
- ASP.NET Core 8 - Web framework for both API and MVC web application
- Entity Framework Core 9.0.7 - ORM for database access
- SignalR - Real-time communication framework for WebSocket/Hub support

**Testing:**
- xUnit 2.9.3 - Unit testing framework
- Moq 4.20.72 - Mocking framework for tests
- FluentAssertions 8.8.0 - Fluent assertion library for cleaner test assertions

**Build/Dev:**
- Visual Studio 17 - IDE configuration
- coverlet 6.0.4 - Code coverage tool (msbuild and collector)
- Microsoft.AspNetCore.Mvc.Testing 8.0.18 - Integration testing utilities

## Key Dependencies

**Critical:**
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 - PostgreSQL database provider for EF Core
- AspNetCore.HealthChecks.NpgSql 9.0.0 - PostgreSQL health check integration
- AspNetCore.HealthChecks.UI 9.0.0 - Health check monitoring dashboard UI
- AspNetCore.HealthChecks.UI.Client 9.0.0 - Health check API client

**Authentication & Security:**
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.21 - JWT authentication scheme
- System.IdentityModel.Tokens.Jwt 8.13.0 - JWT token creation and validation
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.21 - Identity framework for user management
- Microsoft.AspNetCore.Identity.UI 8.0.21 - Built-in identity pages and scaffolding

**Infrastructure:**
- Newtonsoft.Json 13.0.3 - JSON serialization (legacy support)
- Microsoft.EntityFrameworkCore 9.0.7 - Core ORM
- Microsoft.EntityFrameworkCore.Relational 9.0.7 - Relational database abstractions
- Microsoft.EntityFrameworkCore.Design 9.0.7 - CLI tools for migrations
- Microsoft.AspNetCore.Authentication.Cookies 2.3.0 - Cookie-based authentication for web app
- Microsoft.AspNetCore.SignalR.Client 9.0.7 - SignalR client library for web app
- Microsoft.AspNetCore.HttpOverrides - Forwarded headers middleware for reverse proxy support

**Web Framework:**
- Swashbuckle.AspNetCore 9.0.3 - Swagger/OpenAPI documentation generator for API

## Configuration

**Environment:**
- Configured via environment variables in `.env` file
- Fallback values in appsettings.json files (built with implicit usings and nullable reference types enabled)
- Environment detection: `ASPNETCORE_ENVIRONMENT` (Development, Testing, Production)

**Key Configs Required:**
- `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` - PostgreSQL connection details
- `JWT_SECRET` - Minimum 32 characters for token signing
- `JWT_ISSUER` - Token issuer identifier (default: "CrowdQR.Api")
- `JWT_AUDIENCE` - Token audience identifier (default: "CrowdQR.Web")
- `API_PUBLIC_URL` / `WEB_PUBLIC_URL` - External application URLs
- `CORS_ORIGINS` - Comma-separated allowed origins (currently allow all in code)
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED` - For reverse proxy support (Traefik)

**Build:**
- `.csproj` files define project structure and package references
- `Directory.Build.props` for shared build properties
- Visual Studio Solution file: `CrowdQR.sln`

## Platform Requirements

**Development:**
- .NET 8.0 SDK
- PostgreSQL 17 (or compatible)
- Visual Studio 2022 or compatible IDE

**Production:**
- .NET 8.0 Runtime
- PostgreSQL 17 database
- Docker container deployment (via docker-compose.yml)
- Traefik v3.0 reverse proxy for SSL/TLS termination with Cloudflare DNS challenge

## Project Structure

**Solutions:**
- `CrowdQR.sln` - Main solution containing all projects

**Projects:**
- `src/CrowdQR.Api` - ASP.NET Core REST API (net8.0)
  - Entry point: `Program.cs`
  - Health checks at `/health`, `/health/ready`, `/health/live`
  - Controllers: Auth, Dashboard, Event, Request, Session, User, Vote, Test, Reports
  - SignalR hub at `/hubs/crowdqr`

- `src/CrowdQR.Web` - ASP.NET Core Razor Pages MVC application (net8.0)
  - Entry point: `Program.cs`
  - Health checks at `/health`
  - Pages: Login, Admin, Dashboard, Events, Requests, Reports
  - Communicates with API via `ApiService`

- `src/CrowdQR.Shared` - Shared library (net8.0)
  - DTOs, Models, Enums, Utilities shared across API and Web

- `tests/CrowdQR.Api.Tests` - Unit and integration tests
  - Test runner: xUnit
  - In-memory database support via EF Core

---

*Stack analysis: 2026-02-05*
