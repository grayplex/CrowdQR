# Codebase Structure

**Analysis Date:** 2026-02-05

## Directory Layout

```
CrowdQR/
├── src/                        # Source code root
│   ├── CrowdQR.API/           # Backend API project (ASP.NET Core Web)
│   │   ├── Controllers/        # API endpoint controllers
│   │   ├── Data/              # Database context and migrations
│   │   ├── Hubs/              # SignalR hub for real-time updates
│   │   ├── Middleware/        # HTTP pipeline middleware
│   │   ├── Models/            # Domain entities
│   │   ├── Services/          # Business logic services
│   │   ├── Migrations/        # EF Core database migrations
│   │   ├── Properties/        # Project properties
│   │   └── Program.cs         # Application entry point
│   ├── CrowdQR.Shared/        # Shared library (.NET Standard)
│   │   └── Models/
│   │       ├── DTOs/          # Data transfer objects
│   │       └── Enums/         # Shared enumerations
│   └── CrowdQR.Web/           # Frontend web app (ASP.NET Core Razor Pages)
│       ├── Pages/             # Razor page controllers and views
│       │   ├── Admin/         # DJ admin pages (authenticated)
│       │   └── Shared/        # Shared page components
│       ├── Services/          # API client services
│       ├── Extensions/        # Extension methods
│       ├── Models/            # View models
│       ├── Utilities/         # Utility classes
│       ├── wwwroot/           # Static files (CSS, JS, images)
│       │   ├── css/
│       │   ├── js/
│       │   ├── lib/           # Client libraries
│       │   └── sounds/        # Audio files
│       ├── Properties/        # Project properties
│       └── Program.cs         # Application entry point
├── tests/                      # Test projects
│   └── CrowdQR.API.Tests/     # API integration and unit tests
├── docs/                       # Documentation
│   ├── api/                   # API documentation
│   ├── erd/                   # Entity relationship diagrams
│   └── wireframes/            # UI wireframes
├── deploy/                     # Deployment configuration
│   └── traefik/               # Traefik reverse proxy config
├── scripts/                    # Utility scripts
├── .github/                    # GitHub workflows
├── .planning/                  # GSD planning artifacts
│   └── codebase/              # Codebase analysis documents
├── CrowdQR.sln                # Solution file
├── loki-config.yaml           # Loki logging configuration
└── promtail-config.yaml       # Promtail agent configuration
```

## Directory Purposes

**CrowdQR.API:**
- Purpose: RESTful API backend serving the web frontend and external clients
- Contains: Controllers, services, middleware, database models, migrations
- Key files: `Program.cs` (startup), `Data/CrowdQRContext.cs` (EF context)

**CrowdQR.API/Controllers:**
- Purpose: HTTP endpoint handlers for REST API
- Contains: 8 controller classes (Auth, Event, Request, Vote, Session, Dashboard, Reports, Test)
- Pattern: One controller per domain aggregate (User, Event, Request, Vote, Session)
- Key files: `AuthController.cs`, `EventController.cs`, `RequestController.cs`

**CrowdQR.API/Services:**
- Purpose: Business logic and orchestration layer
- Contains: 10 classes (5 interfaces, 5 implementations)
- Services: Authentication, Password hashing, JWT tokens, Email, Hub notifications
- Key files: `AuthService.cs`, `PasswordService.cs`, `TokenService.cs`

**CrowdQR.API/Middleware:**
- Purpose: HTTP pipeline cross-cutting concerns
- Contains: 6 files (3 middleware classes, 3 extension methods)
- Middleware: Exception handling, Authorization logging, DJ role validation
- Pattern: Middleware + extension class per feature for reusability

**CrowdQR.API/Models:**
- Purpose: Domain entities mapped to database tables
- Contains: 5 entity classes (User, Event, Request, Vote, Session) + 1 unused (TrackMetadata)
- Pattern: Entities use EF Core annotations, navigation properties for relationships
- Key files: `User.cs`, `Event.cs`, `Request.cs`, `Vote.cs`, `Session.cs`

**CrowdQR.API/Data:**
- Purpose: Database abstraction and persistence
- Contains: CrowdQRContext (DbContext), DbSeeder (data initialization)
- Key files: `CrowdQRContext.cs` (EF configuration), `DbSeeder.cs` (seed data)

**CrowdQR.API/Migrations:**
- Purpose: Database schema versioning and migrations
- Generated: By EF Core tooling
- Committed: Yes (enables reproducible deployments)
- Key files: `*_InitialCreate.cs`, `CrowdQRContextModelSnapshot.cs`

**CrowdQR.Shared:**
- Purpose: Shared types and contracts between API and Web
- Contains: No dependencies on other projects
- Key subdirectories: Models/DTOs, Models/Enums

**CrowdQR.Shared/Models/DTOs:**
- Purpose: Data transfer objects for API contracts
- Contains: 30+ DTO classes (Auth, Event, Request, User, Vote, Dashboard, Reports)
- Pattern: Read-only properties, Required attributes, documentation comments
- Key files: `AuthLoginDto.cs`, `EventDto.cs`, `RequestDto.cs`, `AuthResultDto.cs`

**CrowdQR.Shared/Models/Enums:**
- Purpose: Shared enumeration types
- Contains: UserRole (DJ, Audience), RequestStatus (Pending, Played, Declined, Completed)
- Key files: `UserRole.cs`, `RequestStatus.cs`

**CrowdQR.Web:**
- Purpose: Public-facing web application and DJ dashboard
- Contains: Razor Pages, page models, services, static files
- Pattern: Page-per-route with code-behind (.cshtml.cs files)

**CrowdQR.Web/Pages:**
- Purpose: Razor page views and handlers
- Contains: 18 page files (public pages + admin pages)
- Subdirectories: Admin/ (protected), Shared/ (layouts)
- Pattern: Folder structure matches URL routes (e.g., /Admin/Dashboard.cshtml → /admin/dashboard)

**CrowdQR.Web/Pages/Admin:**
- Purpose: DJ management and analytics dashboard (requires DJ role)
- Protected: By authorization policy in Program.cs
- Contains: Dashboard, Events, Reports, Settings pages
- Key files: `Dashboard.cshtml.cs`, `Events.cshtml.cs`, `Reports.cshtml.cs`

**CrowdQR.Web/Services:**
- Purpose: API client services (thin wrappers around HTTP client)
- Contains: 10 service classes
- Pattern: Generic ApiService base, specialized services for each domain
- Key files: `ApiService.cs` (base), `AuthenticationService.cs`, `EventService.cs`, `RequestService.cs`

**CrowdQR.Web/Extensions:**
- Purpose: Helper methods for dependency injection and HTTP handling
- Contains: HttpClientLoggingExtensions, HttpClientLoggingHandler
- Pattern: Extension methods registered in Program.cs

**CrowdQR.Web/wwwroot:**
- Purpose: Static assets served by web server
- Subdirectories: css/, js/, lib/, sounds/
- Pattern: Structure mirrors asset types
- Key files: `/lib/` contains third-party libraries (Bootstrap, jQuery, etc.)

**tests/CrowdQR.API.Tests:**
- Purpose: Integration and unit tests for API
- Contains: Test fixtures, test cases
- Pattern: xUnit framework with FluentAssertions
- Key config: `CrowdQR.Api.Tests.csproj`

## Key File Locations

**Entry Points:**
- API: `src/CrowdQR.API/Program.cs` - Configures DI, middleware, database, authentication
- Web: `src/CrowdQR.Web/Program.cs` - Configures DI, services, pages, authentication
- Solution: `CrowdQR.sln` - Three projects: API, Shared, Web

**Configuration:**
- API Database: Connection string built from env vars DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
- API JWT: Secret (JWT_SECRET), Issuer (JWT_ISSUER), Audience (JWT_AUDIENCE)
- Web API Base URL: ApiSettings:BaseUrl from configuration
- Health checks: `/health`, `/health/ready`, `/health/live` endpoints

**Core Logic:**
- Authentication: `src/CrowdQR.API/Services/AuthService.cs` - User login, registration, email verification
- Requests: `src/CrowdQR.API/Controllers/RequestController.cs` - Song request CRUD
- Events: `src/CrowdQR.API/Controllers/EventController.cs` - Event management
- Real-time: `src/CrowdQR.API/Hubs/CrowdQRHub.cs` - SignalR event group management

**Testing:**
- Test fixtures: `tests/CrowdQR.API.Tests/` - Integration test setup, mock data
- xUnit: Used throughout test project

**Database:**
- Context: `src/CrowdQR.API/Data/CrowdQRContext.cs` - DbSets, model configuration
- Migrations: `src/CrowdQR.API/Migrations/` - Version history
- Provider: PostgreSQL via Npgsql EF Core provider

## Naming Conventions

**Files:**

- C# files: PascalCase, one class per file (with rare exceptions like middleware with extensions)
- Domain models: `{Entity}.cs` (User.cs, Event.cs, Request.cs)
- Controllers: `{Feature}Controller.cs` (AuthController.cs, EventController.cs)
- Services (impl): `{Feature}Service.cs` (AuthService.cs, PasswordService.cs)
- Services (interface): `I{Feature}Service.cs` (IAuthService.cs, IPasswordService.cs)
- DTOs: `{Feature}{Type}Dto.cs` (AuthLoginDto.cs, EventCreateDto.cs, UserDto.cs)
- Enums: `{Name}.cs` (UserRole.cs, RequestStatus.cs)
- Middleware: `{Feature}Middleware.cs` (ExceptionHandlingMiddleware.cs)
- Pages: `{Feature}.cshtml` + `{Feature}.cshtml.cs` (Event.cshtml, Login.cshtml)

**Directories:**

- Feature directories (Controllers, Services, Pages): PascalCase
- Sub-features: PascalCase (Admin, Shared)
- Static assets: lowercase (css, js, lib, sounds)
- Enum directories: PascalCase plural (Enums)
- DTO directories: PascalCase plural (DTOs)

## Where to Add New Code

**New API Feature (e.g., new entity/aggregate):**

1. Domain model: `src/CrowdQR.API/Models/{Feature}.cs`
2. DTOs (shared): `src/CrowdQR.Shared/Models/DTOs/{Feature}*Dto.cs` (Create, Read, Update, etc.)
3. Service interface: `src/CrowdQR.API/Services/I{Feature}Service.cs`
4. Service implementation: `src/CrowdQR.API/Services/{Feature}Service.cs`
5. Controller: `src/CrowdQR.API/Controllers/{Feature}Controller.cs`
6. Database configuration: Add DbSet<T> to CrowdQRContext.OnModelCreating()
7. Migration: Run `dotnet ef migrations add {FeatureName}` in API project
8. Tests: Add test class in `tests/CrowdQR.API.Tests/`

**New Web Feature (e.g., page or dashboard section):**

1. Page model and view: `src/CrowdQR.Web/Pages/{Feature}.cshtml` + `.cshtml.cs`
2. For Admin pages: `src/CrowdQR.Web/Pages/Admin/{Feature}.cshtml` + `.cshtml.cs`
3. Page-specific service: `src/CrowdQR.Web/Services/{Feature}Service.cs` (if complex)
4. Add authorization attribute if protected: `[Authorize(Policy = "DjOnly")]`
5. Register service in Program.cs if new: `builder.Services.AddScoped<{Feature}Service>()`

**New Middleware/Cross-cutting Concern:**

1. Middleware class: `src/CrowdQR.API/Middleware/{Feature}Middleware.cs`
2. Extension method: `src/CrowdQR.API/Middleware/{Feature}MiddlewareExtensions.cs`
3. Register in Program.cs: `app.Use{Feature}()` in appropriate pipeline position
4. Pattern: Keep middleware focused on single responsibility

**New Shared Type:**

1. DTOs: `src/CrowdQR.Shared/Models/DTOs/{Type}Dto.cs`
2. Enums: `src/CrowdQR.Shared/Models/Enums/{Type}.cs`
3. No dependencies outside of System.*

## Special Directories

**src/CrowdQR.API/Migrations:**
- Purpose: Database schema evolution tracking
- Generated: By EF Core (dotnet ef migrations add)
- Committed: Yes - essential for reproducible deployments
- Pattern: Timestamp_MigrationName.cs, Designer.cs pair, ModelSnapshot.cs for current state

**src/CrowdQR.Web/wwwroot:**
- Purpose: Static files served directly by web server
- Generated: No (hand-managed and third-party libraries)
- Committed: Yes (for CSS, JS, fonts)
- Pattern: Mirrors asset types (css/, js/, lib/) for organization

**tests/CrowdQR.API.Tests:**
- Purpose: Test code (unit, integration, fixtures)
- Generated: No
- Committed: Yes
- Pattern: Mirrors source structure with Test suffix on class names

**.planning/codebase:**
- Purpose: Codebase analysis and planning artifacts generated by GSD tools
- Generated: Yes (by /gsd commands)
- Committed: Yes (for team reference)
- Pattern: ARCHITECTURE.md, STRUCTURE.md, CONVENTIONS.md, TESTING.md, CONCERNS.md, STACK.md, INTEGRATIONS.md

