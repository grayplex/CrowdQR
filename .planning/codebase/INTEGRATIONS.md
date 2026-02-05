# External Integrations

**Analysis Date:** 2026-02-05

## APIs & External Services

**Reverse Proxy:**
- Traefik v3.0 - Handles HTTP/HTTPS routing, SSL/TLS termination, and load balancing
  - Configuration: `deploy/traefik/` directory
  - Supports dynamic routing and certificate management

**Content Delivery:**
- Cloudflare DNS API - Automatic DNS challenge for Let's Encrypt SSL certificates
  - Environment vars: `CLOUDFLARE_DNS_API_TOKEN`, `CLOUDFLARE_EMAIL`
  - Used by Traefik for ACME DNS challenge

**Future Integrations (Planned, Not Implemented):**
- Spotify API - Commented stub in `src/CrowdQR.Api/Data/CrowdQRContext.cs` lines 42-47
  - Entity defined but disabled: `TrackMetadata` model (commented out)
  - Would store track metadata (Spotify ID, YouTube ID, album art URLs)
  - Status: TODO - Not yet implemented in the CrowdQR project

## Data Storage

**Databases:**
- PostgreSQL 17
  - Connection: Environment variables `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
  - Default connection string pattern: `Host={host};Port={port};Database={database};Username={username};Password={password}`
  - Client: Entity Framework Core 9.0.7 with Npgsql.EntityFrameworkCore.PostgreSQL provider
  - Health check: AspNetCore.HealthChecks.NpgSql 9.0.0
  - Container: `postgres:17` in docker-compose

**File Storage:**
- Local filesystem only (no remote storage integration)

**Caching:**
- Distributed memory cache (ASP.NET Core in-memory)
- Session state: `AddDistributedMemoryCache()` in `src/CrowdQR.Web/Program.cs` line 121

**ORM/Data Access:**
- Entity Framework Core 9.0.7 - Primary ORM
- Context: `CrowdQRContext` in `src/CrowdQR.Api/Data/CrowdQRContext.cs`
- Tables: User, Event, Request, Vote, Session
- Migrations: Stored in `src/CrowdQR.Api/Migrations/`

## Authentication & Identity

**Auth Provider:**
- Custom JWT-based authentication
  - Issuer: Environment var `JWT_ISSUER` (default: "CrowdQR.Api")
  - Audience: Environment var `JWT_AUDIENCE` (default: "CrowdQR.Web")
  - Secret: Environment var `JWT_SECRET` (minimum 32 characters)
  - Token validation: `System.IdentityModel.Tokens.Jwt` 8.13.0

**Implementation:**
- API (`src/CrowdQR.Api/Program.cs` lines 45-98):
  - Bearer token authentication via `JwtBearerDefaults.AuthenticationScheme`
  - Token validation parameters with issuer and audience checks
  - JWT events logging: `OnAuthenticationFailed`, `OnTokenValidated`, `OnMessageReceived`

- Web Application (`src/CrowdQR.Web/Program.cs` lines 45-68):
  - Dual cookie schemes: "WebAppCookie" (primary) and "AudienceCookie"
  - Login path: `/Login`
  - Logout path: `/Logout`
  - Access denied path: `/AccessDenied`
  - Cookie expiration: 1 hour (with sliding expiration)

**Token Service:**
- `src/CrowdQR.Api/Services/TokenService.cs` - Generates and validates JWT tokens
- Password hashing: `src/CrowdQR.Api/Services/PasswordService.cs` - Secure password operations

**Authorization Policies:**
- API: Role-based access control (DJ role required for certain endpoints)
- Web: "DjOnly" policy for admin pages (`options.Conventions.AuthorizeFolder("/Admin", "DjOnly")`)
- Web: "AudienceAccess" policy for session-based access

## Monitoring & Observability

**Error Tracking:**
- Not detected - No integrated error tracking service (Sentry, Application Insights, etc.)

**Logs:**
- ASP.NET Core built-in logging
  - Console output
  - Debug output
  - Log level: Information (configurable via `LOG_LEVEL` env var)
  - Minimum level in Web: Debug (`builder.Logging.SetMinimumLevel(LogLevel.Debug)`)
  - Custom middleware logging: `src/CrowdQR.Api/Middleware/AuthorizationLoggingMiddleware.cs`

**Health Checks:**
- AspNetCore.HealthChecks frameworks
- API endpoints:
  - `/health` - Overall health with detailed status
  - `/health/ready` - Readiness probe (tagged checks)
  - `/health/live` - Liveness probe
  - PostgreSQL health: Checks database connectivity
  - Self-health: Custom check
- Web endpoints:
  - `/health` - Overall health
  - Checks API health at `{ApiSettings:BaseUrl}/health`

**Observability (Infrastructure):**
- Loki configuration: `loki-config.yaml` - Log aggregation (configured but integration not found in app code)
- Promtail configuration: `promtail-config.yaml` - Log forwarder (configured but integration not found in app code)
- Health check UI: AspNetCore.HealthChecks.UI 9.0.0 (available but endpoint location not explicitly configured in code)

## CI/CD & Deployment

**Hosting:**
- Docker containers via docker-compose
  - API service: `crowdqr-api` at port 5000 (internal)
  - Web service: `crowdqr-web` at port 80 (internal)
  - Database: `crowdqr-db` postgres:17
  - Traefik: `crowdqr-traefik` at ports 80, 443

**Container Registries:**
- GitHub Container Registry (GHCR)
  - Registry URL: Environment var `REGISTRY` (default: "ghcr.io")
  - Repository: Environment var `REPOSITORY` (default: "grayplex/crowdqr")
  - Image tags: Environment var `CROWDQR_VERSION` (default: "latest")

**CI Pipeline:**
- DeepSource integration: `.deepsource.toml` for code quality analysis
- GitHub Actions: Implicit via `.github/dependabot.yml` for dependency updates
- Dependabot: Automated dependency updates for NuGet packages

**Deployment Networking:**
- Docker network: `crowdqr-network` (bridge driver)
- Traefik labels: Dynamic routing based on Docker labels
- CORS: Configured in API to allow all origins (code: `AllowAnyOrigin()` is effectively enabled)
- Forwarded headers: Enabled for reverse proxy (Traefik)

## Environment Configuration

**Required env vars:**
- `POSTGRES_USER` - Database user
- `POSTGRES_PASSWORD` - Database password (minimum security requirement: change in production)
- `POSTGRES_DB` - Database name
- `ASPNETCORE_ENVIRONMENT` - Environment mode
- `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` - Connection parameters
- `JWT_SECRET` - Minimum 32 characters, cryptographically secure random string
- `JWT_ISSUER` - Token issuer
- `JWT_AUDIENCE` - Token audience
- `JWT_EXPIRY_HOURS` - Token lifetime (default: 24)
- `CLOUDFLARE_DNS_API_TOKEN` - Cloudflare API access
- `CLOUDFLARE_EMAIL` - Cloudflare account email
- `API_PUBLIC_URL` - Production API URL
- `WEB_PUBLIC_URL` - Production web URL
- `CORS_ORIGINS` - Comma-separated allowed origins
- `LOG_LEVEL` - Logging verbosity level

**Optional env vars:**
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME` - Email configuration (not implemented)
- `RATE_LIMIT_REQUESTS_PER_MINUTE` - Rate limiting config (not implemented)
- `HEALTH_CHECK_TIMEOUT` - Health check timeout in seconds
- Docker compose overrides: `DOCKER_API_PORT`, `DOCKER_WEB_PORT`, `DOCKER_DB_PORT`
- Development overrides: `DEV_DB_HOST`, `DEV_DB_PORT`, `DEV_DB_NAME`, `DEV_DB_USER`, `DEV_DB_PASSWORD`, `DEV_API_URL`, `DEV_WEB_URL`

**Secrets location:**
- `.env` file (Git ignored via `.gitignore`)
- `.env.example` provided as template with default values and security notes

## Webhooks & Callbacks

**Incoming:**
- Not detected - No webhook endpoints defined

**Outgoing:**
- Not detected - No outbound webhook calls

## Real-Time Communication

**SignalR Hub:**
- Endpoint: `/hubs/crowdqr` mapped in `src/CrowdQR.Api/Program.cs` line 230
- Hub class: `src/CrowdQR.Api/Hubs/CrowdQRHub.cs`
- Methods:
  - `JoinEvent(int eventId)` - Subscribe to event updates
  - `LeaveEvent(int eventId)` - Unsubscribe from event
  - `Ping()` - Connection health check
- Server-sent events:
  - `userJoinedEvent` - User joined event group
  - `userLeftEvent` - User left event group
- Client library: Microsoft.AspNetCore.SignalR.Client 9.0.7 (used in Web app)
- Message size limit: 100 KB (102400 bytes)

## Email Integration

**Status:**
- Placeholder implementation in `src/CrowdQR.Api/Services/EmailService.cs`
- Environment variables defined but not functional
- Current behavior: Logs to console and ILogger instead of sending actual emails
- Verification emails: Generates URL and logs it
- Password reset emails: Logs reset URL

---

*Integration audit: 2026-02-05*
