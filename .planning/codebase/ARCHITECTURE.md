# Architecture

**Analysis Date:** 2026-02-05

## Pattern Overview

**Overall:** Tiered N-layer architecture with clear separation of concerns across three projects: API backend, shared models, and MVC web frontend.

**Key Characteristics:**
- Dependency Injection (DI) driven service architecture
- Entity Framework Core for database abstraction
- JWT token-based authentication with cookie-based session management in web layer
- SignalR for real-time event notifications
- DTOs for API contract abstraction from domain models
- Middleware pipeline for cross-cutting concerns
- Reverse proxy ready (Traefik-compatible) deployment model

## Layers

**Presentation Layer (Web):**
- Purpose: Razor Pages user interface, public-facing web application
- Location: `src/CrowdQR.Web/Pages`
- Contains: Page models (.cshtml.cs files), public website, admin dashboard, event pages
- Depends on: Shared models (DTOs), Web services, API backend via HTTP
- Used by: End users (browsers)
- Key pattern: Page-based view models using .NET dependency injection

**API Layer (Controllers):**
- Purpose: RESTful API endpoints for all business operations
- Location: `src/CrowdQR.API/Controllers`
- Contains: AuthController, EventController, RequestController, VoteController, SessionController, DashboardController, ReportsController
- Depends on: Services, Data context, Shared DTOs
- Used by: Web frontend, external clients via HTTP

**Service Layer:**
- Purpose: Business logic and orchestration
- Location: `src/CrowdQR.API/Services`
- Contains: Authentication (AuthService), Password hashing (PasswordService), JWT token generation (TokenService), Email notifications (EmailService), Real-time updates (HubNotificationService)
- Depends on: Data context, Configuration, External services
- Used by: Controllers, Middleware

**Data Layer (EF Core):**
- Purpose: Database persistence and ORM abstraction
- Location: `src/CrowdQR.API/Data/CrowdQRContext.cs`
- Contains: DbContext, DbSets for User, Event, Request, Vote, Session
- Depends on: PostgreSQL via Npgsql provider
- Used by: Services, Controllers directly in some cases (legacy pattern)

**Shared Models Layer:**
- Purpose: DTOs and enums shared across API and Web
- Location: `src/CrowdQR.Shared/Models`
- Contains: DTOs (AuthLoginDto, EventDto, RequestDto, etc.), Enums (UserRole, RequestStatus)
- Depends on: System libraries only
- Used by: API, Web, both for contract definitions

**Middleware Layer:**
- Purpose: Cross-cutting concerns and pipeline processing
- Location: `src/CrowdQR.API/Middleware`
- Contains: ExceptionHandlingMiddleware, AuthorizationLoggingMiddleware, DjRoleValidationMiddleware
- Depends on: AspNetCore pipeline
- Used by: Program.cs during request pipeline configuration

**SignalR Hub Layer:**
- Purpose: Real-time bidirectional communication
- Location: `src/CrowdQR.API/Hubs/CrowdQRHub.cs`
- Contains: Event group management, real-time notifications
- Depends on: AspNetCore.SignalR
- Used by: Web frontend via WebSocket connections

## Data Flow

**User Registration Flow:**

1. User submits registration form on `src/CrowdQR.Web/Pages/Register.cshtml.cs`
2. AuthenticationService in Web calls API endpoint `POST /api/auth/register`
3. AuthController receives request with AuthDjRegisterDto
4. AuthService (API) validates and calls PasswordService to hash password
5. User entity is created and persisted via CrowdQRContext
6. AuthService calls TokenService to generate JWT token
7. AuthResultDto returned with token
8. Web stores token in session, sets authentication cookie
9. User is redirected to authenticated area

**Request Submission Flow:**

1. Audience user on event page submits song request via form on `src/CrowdQR.Web/Pages/Event.cshtml.cs`
2. RequestService (Web) calls API endpoint `POST /api/request`
3. RequestController creates Request entity via context
4. CrowdQRContext persists to PostgreSQL
5. HubNotificationService broadcasts update to all connected clients in event group
6. Real-time update sent via SignalR to CrowdQRHub
7. All connected clients receive updated request list via WebSocket

**Voting Flow:**

1. User clicks vote button on event page
2. VoteService calls API endpoint `POST /api/vote`
3. VoteController creates Vote entity
4. Database unique constraint (one_vote_per_user) enforced at DB level
5. HubNotificationService notifies event group of vote count change
6. SignalR broadcasts updated vote counts

**State Management:**

- **API State:** Transient and scoped services manage request-scoped state; database is source of truth
- **Web State:** Session state via `IDistributedMemoryCache` (can be scaled to Redis); HTTP cookies for authentication
- **Audience Sessions:** SessionManager tracks session per user per event via Session table
- **Real-time State:** SignalR groups organize clients by event; no state persistence

## Key Abstractions

**Service Interfaces:**

- `IAuthService`: Authentication, registration, email verification
  - Examples: `src/CrowdQR.API/Services/IAuthService.cs`
  - Pattern: Dependency injection, single responsibility

- `ITokenService`: JWT token generation and validation
  - Examples: `src/CrowdQR.API/Services/ITokenService.cs`
  - Pattern: Configuration-driven (JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE env vars)

- `IPasswordService`: Password hashing and verification
  - Examples: `src/CrowdQR.API/Services/IPasswordService.cs`
  - Pattern: Stateless, uses PBKDF2 with salt

- `IEmailService`: Email sending
  - Examples: `src/CrowdQR.API/Services/IEmailService.cs`
  - Pattern: Async, exception-safe

- `IHubNotificationService`: Real-time event notifications
  - Examples: `src/CrowdQR.API/Services/IHubNotificationService.cs`
  - Pattern: Orchestrates SignalR client calls

**Domain Models:**

- `User`: Users (DJ or Audience role), navigation to events, requests, votes, sessions
  - File: `src/CrowdQR.API/Models/User.cs`
  - Roles: UserRole.DJ (requires password), UserRole.Audience (optional password)

- `Event`: DJ-hosted events, tracks host DJ, all requests and sessions
  - File: `src/CrowdQR.API/Models/Event.cs`
  - Key field: Slug (URL-friendly unique identifier)

- `Request`: Song requests, linked to User and Event, tracks status and votes
  - File: `src/CrowdQR.API/Models/Request.cs`
  - Status: Pending, Played, Declined, Completed (RequestStatus enum)

- `Vote`: User's vote for a Request (one vote per user per request constraint)
  - File: `src/CrowdQR.API/Models/Vote.cs`
  - Constraint: Unique index on (UserId, RequestId)

- `Session`: Tracks active user sessions per event for rate limiting
  - File: `src/CrowdQR.API/Models/Session.cs`
  - Constraint: One session per user per event

**Web Service Layer:**

- `ApiService`: Generic HTTP client wrapper with token attachment
  - File: `src/CrowdQR.Web/Services/ApiService.cs`
  - Pattern: GetAsync<T>, PostAsync<T>, PutAsync<T>, DeleteAsync with logging

- `AuthenticationService`: JWT token management and cookie handling
  - File: `src/CrowdQR.Web/Services/AuthenticationService.cs`
  - Pattern: Manages token refresh, stores in session

- Specific Services: EventService, RequestService, VoteService, SessionService, DashboardService, ReportService
  - Location: `src/CrowdQR.Web/Services/`
  - Pattern: Thin wrappers around ApiService with typed endpoints

## Entry Points

**API Entry Point:**
- Location: `src/CrowdQR.API/Program.cs`
- Triggers: Application startup
- Responsibilities:
  - Configure DbContext with PostgreSQL connection
  - Register all services (Auth, Password, Token, Email, Hub)
  - Configure JWT authentication scheme with Bearer tokens
  - Configure SignalR with max message size of 100KB
  - Apply database migrations (non-development environments)
  - Configure CORS policy "AllowWebApp" with credentials
  - Map middleware pipeline: Exception handling, CORS, Auth, Authorization, Custom validation
  - Map SignalR hub at `/hubs/crowdqr`
  - Map health check endpoints

**Web Entry Point:**
- Location: `src/CrowdQR.Web/Program.cs`
- Triggers: Application startup
- Responsibilities:
  - Configure HTTP client factory with base address from config
  - Register dual authentication schemes (WebAppCookie, AudienceCookie)
  - Configure authorization policies (DjOnly, AudienceAccess)
  - Register all Web service classes (ApiService, EventService, etc.)
  - Configure reverse proxy headers (Traefik compatible)
  - Configure distributed memory cache for sessions
  - Enable Razor Pages with admin folder auth policy
  - Map health check endpoints
  - Map custom theme endpoint POST /api/theme

**Controller Entry Points:**

- `AuthController` (`src/CrowdQR.API/Controllers/AuthController.cs`): `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/verify-email`
- `EventController` (`src/CrowdQR.API/Controllers/EventController.cs`): `GET /api/event`, `GET /api/event/{id}`, `GET /api/event/slug/{slug}`, `POST /api/event` (DJ only)
- `RequestController`: Song request CRUD and status management
- `VoteController`: Vote submission and retrieval
- `SessionController`: Session tracking for rate limiting
- `DashboardController`: Analytics and summaries for DJs
- `ReportsController`: Event performance and usage analytics

## Error Handling

**Strategy:** Centralized exception handling with logging and standardized JSON error responses

**Patterns:**

- **Global Middleware Catch** (`ExceptionHandlingMiddleware`):
  - Catches all unhandled exceptions in request pipeline
  - Logs exception details
  - Returns HTTP 500 with JSON: `{ status, message, detail }`
  - Located: `src/CrowdQR.API/Middleware/ExceptionHandlingMiddleware.cs`

- **Controller-Level Validation:**
  - ModelState validation on request objects
  - Manual parameter validation (required checks)
  - Returns HTTP 400 BadRequest with error messages

- **Business Logic Validation:**
  - AuthService returns AuthResultDto with Success flag and ErrorMessage
  - Services return typed DTOs with embedded status/error info
  - Controllers inspect response and map to appropriate HTTP status codes

- **JWT Authentication Errors:**
  - Invalid token: HTTP 401 Unauthorized
  - Expired token: HTTP 401 Unauthorized
  - JWT events configured in Program.cs log failed authentication attempts

- **Database Constraints:**
  - Unique constraint violations (duplicate username, unique slug) caught at EF level
  - Foreign key cascades configured on delete

## Cross-Cutting Concerns

**Logging:**
- Framework: Microsoft.Extensions.Logging
- Configuration: Console and Debug providers enabled
- Minimum level: Debug in development, varies by environment
- Usage: ILogger<T> injected into services and controllers
- HTTP client logging: Custom HttpClientLoggingHandler logs request/response

**Validation:**
- Data annotations on DTOs (Required, MaxLength, etc.)
- ModelState validation in controllers
- Business logic validation in services
- Database-level constraints (unique indexes, foreign keys)

**Authentication:**
- Scheme: JWT Bearer tokens for API
- Configuration: IssuerSigningKey, ValidIssuer, ValidAudience, ValidateLifetime
- Secret source: JWT_SECRET env var (min 32 chars), fallback in config
- Events: OnAuthenticationFailed, OnTokenValidated, OnMessageReceived for logging
- Token generation: ITokenService creates signed tokens with user claims

**Authorization:**
- Attribute-based: [Authorize], [AllowAnonymous]
- Policy-based: "DjOnly" policy checks UserRole.DJ claim
- Custom middleware: DjRoleValidationMiddleware enforces DJ-only endpoints
- Web app policies: "DjOnly" for Admin folder, "AudienceAccess" for event pages

**Rate Limiting:**
- Tracking via Session table (ClientIP, RequestCount, LastSeen)
- SessionService increments RequestCount per session
- Implementation incomplete (business logic in place, not fully enforced)

