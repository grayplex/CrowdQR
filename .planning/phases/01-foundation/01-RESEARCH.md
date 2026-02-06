# Phase 1: Foundation - Research

**Researched:** 2026-02-05
**Domain:** ASP.NET Core async patterns, EF Core query optimization, PostgreSQL indexing
**Confidence:** HIGH

## Summary

This phase focuses on establishing performance fundamentals for the CrowdQR application: async/await patterns throughout the call stack, EF Core query optimization to eliminate N+1 patterns, database indexing, and connection pool management. The research confirms that ASP.NET Core 8.0 and EF Core 9.0 provide mature, well-documented approaches to these challenges.

The standard approach is bottom-up async migration (data layer → services → controllers), strict eager loading with `.Include()` to prevent N+1 queries, and comprehensive profiling with EF Core query logging + MiniProfiler during development + Application Insights in production. ASP.NET Core's removal of `SynchronizationContext` simplifies async/await patterns compared to older frameworks, making ConfigureAwait largely unnecessary in application code (though project-level settings can enforce consistency).

**Primary recommendation:** Combine async/await conversion with query optimization in the same pass—fixing data layer queries while converting to async prevents rework and surfaces N+1 patterns immediately through profiling tools.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core | 9.0.7 (current) | ORM and query optimization | Official Microsoft ORM with mature async support and comprehensive profiling |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 (current) | PostgreSQL provider for EF Core | Official PostgreSQL provider with built-in connection pooling |
| Microsoft.Extensions.Logging | Built-in (8.0+) | EF Core query logging | Native logging integration with EF Core diagnostics |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| MiniProfiler.AspNetCore.Mvc | 4.x+ | Request-level query profiling | Development environment for visualizing query counts and timings |
| Microsoft.ApplicationInsights.AspNetCore | 2.x+ | APM and production monitoring | Production environment for tracking query performance and dependencies |
| AsyncFixer | Latest | Roslyn analyzer for async anti-patterns | Build-time enforcement of async best practices |
| Microsoft.VisualStudio.Threading.Analyzers | Latest | Async/threading static analysis | Build-time detection of deadlocks and threading issues |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| MiniProfiler | Glimpse | Glimpse is discontinued; MiniProfiler is actively maintained |
| Application Insights | Datadog/New Relic | Application Insights integrates natively with Azure and has lower cost for .NET apps |
| EF Core query logging | SQL Profiler | Query logging is code-integrated and doesn't require database access |

**Installation:**
```bash
dotnet add package MiniProfiler.AspNetCore.Mvc
dotnet add package Microsoft.ApplicationInsights.AspNetCore
dotnet add package AsyncFixer
dotnet add package Microsoft.VisualStudio.Threading.Analyzers
```

## Architecture Patterns

### Recommended Async Migration Order (Bottom-Up)
Convert layers in this sequence to avoid sync-over-async anti-patterns:

```
1. Data Layer (DbContext operations)
   ├── ToListAsync(), FirstOrDefaultAsync(), SaveChangesAsync()
   └── All LINQ query terminating operators

2. Service Layer
   ├── Business logic methods
   └── All calls to data layer (await async methods)

3. Controller Layer
   ├── API endpoints (return Task<IActionResult>)
   └── All calls to services (await async methods)
```

### Pattern 1: Async Data Access with EF Core
**What:** All database operations use async methods with proper disposal patterns
**When to use:** Every database query and save operation
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying
public async Task<Event> GetEventByIdAsync(int eventId)
{
    await using var context = await _contextFactory.CreateDbContextAsync();

    return await context.Events
        .AsNoTracking()
        .Include(e => e.Requests)
            .ThenInclude(r => r.Votes)
        .FirstOrDefaultAsync(e => e.Id == eventId);
}
```

### Pattern 2: Eager Loading to Prevent N+1 Queries
**What:** Use `.Include()` and `.ThenInclude()` to load related entities in a single query
**When to use:** When navigation properties will be accessed after query execution
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying

// ❌ BAD: Lazy loading triggers N+1 queries
var events = await context.Events.ToListAsync();
foreach (var evt in events)
{
    // Each access to Requests triggers a separate query
    Console.WriteLine(evt.Requests.Count);
}

// ✅ GOOD: Eager loading with single query
var events = await context.Events
    .Include(e => e.Requests)
    .ToListAsync();
foreach (var evt in events)
{
    Console.WriteLine(evt.Requests.Count); // No additional query
}
```

### Pattern 3: Projections for Read-Only DTOs
**What:** Use `.Select()` to fetch only required columns instead of entire entities
**When to use:** API responses that don't need the full entity graph
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying

// ❌ BAD: Fetches all columns from Events and Requests
var events = await context.Events
    .Include(e => e.Requests)
    .AsNoTracking()
    .ToListAsync();

// ✅ GOOD: Fetches only needed columns
var events = await context.Events
    .Select(e => new EventDto
    {
        Id = e.Id,
        Name = e.Name,
        RequestCount = e.Requests.Count
    })
    .ToListAsync();
```

### Pattern 4: AsNoTracking for Read-Only Queries
**What:** Disable change tracking for queries that won't update entities
**When to use:** All read-only API endpoints (GET requests)
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/querying/tracking

// ✅ Read-only query with AsNoTracking (29% faster, 39% less memory)
var events = await context.Events
    .AsNoTracking()
    .Where(e => e.IsActive)
    .ToListAsync();
```

### Pattern 5: AsSplitQuery for Multiple Collections
**What:** Split queries loading multiple one-to-many relationships to avoid cartesian explosion
**When to use:** Queries that `.Include()` multiple collection navigation properties
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying

// ❌ BAD: Single JOIN creates cartesian explosion (Event data duplicated per Request × per Session)
var events = await context.Events
    .Include(e => e.Requests)
    .Include(e => e.Sessions)
    .ToListAsync();

// ✅ GOOD: Split into separate queries (no data duplication)
var events = await context.Events
    .AsSplitQuery()
    .Include(e => e.Requests)
    .Include(e => e.Sessions)
    .ToListAsync();
```

### Pattern 6: DbContext Pooling for Performance
**What:** Reuse DbContext instances instead of creating new ones per request
**When to use:** ASP.NET Core dependency injection registration
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics

// ❌ BAD: Creates new context per request
services.AddDbContext<CrowdQRContext>(options =>
    options.UseNpgsql(connectionString));

// ✅ GOOD: Reuses pooled contexts (2x faster, 10x less memory allocation)
services.AddDbContextPool<CrowdQRContext>(options =>
    options.UseNpgsql(connectionString));
```

### Pattern 7: Query Logging Configuration
**What:** Enable EF Core query logging in development via appsettings.json
**When to use:** Development environment to detect N+1 and slow queries
**Example:**
```json
// Source: https://dev.to/kenakamu/entity-framework-core-logging-1hl9
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Query": "Information"
    }
  }
}
```

### Pattern 8: MiniProfiler Setup
**What:** UI-based profiling for query counts and timings per request
**When to use:** Development environment, disabled in production
**Example:**
```csharp
// Source: https://miniprofiler.com/dotnet/AspDotNetCore

// Program.cs or Startup.cs
builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
    options.ResultsAuthorize = _ => builder.Environment.IsDevelopment();
}).AddEntityFramework(); // EF Core integration

app.UseMiniProfiler();

// _Layout.cshtml
<mini-profiler />
```

### Anti-Patterns to Avoid
- **Async void methods:** Use `Task` or `Task<T>` instead—async void crashes the process on unhandled exceptions
- **Blocking with .Result or .Wait():** Causes thread pool starvation and deadlocks—use `await` instead
- **Lazy loading in production:** Enables accidental N+1 queries—disable and use explicit eager loading
- **ConfigureAwait(false) everywhere:** Unnecessary in ASP.NET Core (no SynchronizationContext)—use only in library code
- **Synchronous over async:** Converting sync code to async without updating callers negates benefits and wastes threads

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Query performance profiling | Custom SQL logging to files | MiniProfiler + EF Core query logging | MiniProfiler provides UI, query grouping, and automatic duplication detection; EF logging integrates with appsettings.json |
| Connection pooling | Custom connection manager | Npgsql built-in pooling + DbContext pooling | Npgsql handles connection lifecycle automatically; DbContext pooling adds instance reuse (2x faster) |
| Async static analysis | Manual code reviews | AsyncFixer + Microsoft.VisualStudio.Threading.Analyzers | Roslyn analyzers catch 24+ async anti-patterns at build time with actionable fixes |
| Database index analysis | Manual query inspection | `EXPLAIN ANALYZE` + pg_stat_statements | PostgreSQL built-in tools show actual execution plans and index usage statistics |
| Async cancellation | Manual timeout tracking | CancellationToken in controller/service signatures | ASP.NET Core provides cancellation tokens automatically from request lifetime |

**Key insight:** EF Core and ASP.NET Core have mature profiling and async infrastructure built-in—custom solutions duplicate effort and miss edge cases (e.g., Npgsql connection pooling handles health checks, timeouts, and connection string parsing correctly).

## Common Pitfalls

### Pitfall 1: Sync-Over-Async Anti-Pattern
**What goes wrong:** Calling `.Result` or `.Wait()` on async methods blocks the calling thread, causing thread pool starvation under load and potential deadlocks
**Why it happens:** Developers try to call async methods from synchronous code without propagating async up the call stack
**How to avoid:** Convert entire call stack to async (bottom-up: data → services → controllers); use static analyzers to fail builds on violations
**Warning signs:** Thread pool starvation logs, high thread counts, increased response times under load

### Pitfall 2: N+1 Query Pattern from Lazy Loading
**What goes wrong:** Accessing navigation properties triggers individual SELECT queries per entity (1 query for parent + N queries for children)
**Why it happens:** Lazy loading is enabled or developers assume EF Core loads related data automatically without `.Include()`
**How to avoid:** Disable lazy loading globally in DbContext configuration; mandate `.Include()` for all navigation property access; enable query logging to detect duplicate queries
**Warning signs:** Console logs show identical SELECT statements with different WHERE clauses; query count grows linearly with result count

### Pitfall 3: Cartesian Explosion from Multiple Includes
**What goes wrong:** Including multiple collection navigation properties in a single query creates a JOIN that duplicates parent data (Event data repeated for every Request × Session combination)
**Why it happens:** EF Core default behavior is single query with JOIN; developers don't realize data duplication cost
**How to avoid:** Use `.AsSplitQuery()` when including 2+ collection navigations; monitor query result set size; profile memory allocation
**Warning signs:** Queries return far more rows than entities; high memory usage; slow materialization despite indexed columns

### Pitfall 4: Missing AsNoTracking on Read-Only Queries
**What goes wrong:** Change tracker allocates memory and CPU to track entities that will never be modified, wasting 39% more memory and 29% more time
**Why it happens:** Developers forget that EF Core tracks all entities by default; assume read queries are automatically optimized
**How to avoid:** Apply `.AsNoTracking()` to all GET endpoint queries; consider setting `QueryTrackingBehavior.NoTracking` as default; verify in code reviews
**Warning signs:** High memory allocation in profiler for read endpoints; GC pressure

### Pitfall 5: Ignoring Foreign Key Indexes
**What goes wrong:** Queries joining tables on foreign keys perform full table scans, causing slow JOIN operations and high database CPU
**Why it happens:** PostgreSQL automatically indexes primary keys but NOT foreign keys; developers assume EF Core creates indexes automatically
**How to avoid:** Index all foreign key columns; use `EXPLAIN ANALYZE` to verify index usage; monitor PostgreSQL slow query logs
**Warning signs:** `Seq Scan` in EXPLAIN ANALYZE output; high database CPU; query times increase linearly with table size

### Pitfall 6: Async Method Without Await
**What goes wrong:** Async method returns Task but doesn't await internal async calls, causing disposal issues and incorrect exception propagation
**Why it happens:** Developers return Task directly from another async method without adding `await`
**How to avoid:** Enable compiler warnings for async methods without await; use AsyncFixer analyzer; code review for orphaned async methods
**Warning signs:** Compiler warning CS1998 ("async method lacks 'await' operators"); ObjectDisposedException in logs

### Pitfall 7: Query Cache Pollution from Non-Parameterized Queries
**What goes wrong:** EF Core compiles a new query plan for every unique constant value, exhausting the query cache and slowing down compilation
**Why it happens:** Developers embed constants directly in LINQ expressions instead of using variables
**How to avoid:** Use variables for filter values; monitor EF Core metrics for `Query Cache Hit Rate` (should reach ~100% after startup); avoid dynamic LINQ with constants
**Warning signs:** Low query cache hit rate; increased CPU for query compilation; first-query-per-value slowness

## Code Examples

Verified patterns from official sources:

### Async Controller with Proper Disposal
```csharp
// Source: https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md
[ApiController]
[Route("api/[controller]")]
public class EventController : ControllerBase
{
    private readonly IDbContextFactory<CrowdQRContext> _contextFactory;

    public EventController(IDbContextFactory<CrowdQRContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        // ✅ Proper async disposal with await using
        await using var context = await _contextFactory.CreateDbContextAsync();

        var evt = await context.Events
            .AsNoTracking()
            .Include(e => e.Requests)
                .ThenInclude(r => r.Votes)
            .FirstOrDefaultAsync(e => e.Id == id);

        return evt == null ? NotFound() : Ok(evt);
    }
}
```

### Compiled Query for Hot Paths
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics
public class EventRepository
{
    // ✅ Compiled query defined once, reused across requests (10-40% faster)
    private static readonly Func<CrowdQRContext, string, Task<Event?>> _getEventBySlug =
        EF.CompileAsyncQuery((CrowdQRContext context, string slug) =>
            context.Events
                .AsNoTracking()
                .Include(e => e.Requests.Where(r => r.Status == RequestStatus.Pending))
                .FirstOrDefault(e => e.Slug == slug));

    public async Task<Event?> GetActiveEventBySlugAsync(string slug)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _getEventBySlug(context, slug);
    }
}
```

### EF Core Logging in appsettings.Development.json
```json
// Source: https://dev.to/kenakamu/entity-framework-core-logging-1hl9
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=crowdqr;Username=postgres;Password=postgres"
  }
}
```

### MiniProfiler Configuration with EF Core Integration
```csharp
// Source: https://miniprofiler.com/dotnet/AspDotNetCore
var builder = WebApplication.CreateBuilder(args);

// ✅ MiniProfiler with EF Core profiling enabled
builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
    options.ResultsAuthorize = _ => builder.Environment.IsDevelopment();
    options.ShouldProfile = _ => builder.Environment.IsDevelopment();
}).AddEntityFramework();

var app = builder.Build();

app.UseMiniProfiler(); // ⚠️ Must come before UseEndpoints/MapControllers

app.MapControllers();
app.Run();
```

### Disabling Lazy Loading and Enabling Query Logging
```csharp
// Source: https://learn.microsoft.com/en-us/ef/core/querying/related-data/lazy
public class CrowdQRContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // ✅ Disable lazy loading to prevent N+1 queries
        optionsBuilder.UseLazyLoadingProxies(false);

        // ✅ Enable sensitive data logging in development only
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        }
    }
}
```

### Index Creation for Foreign Keys
```csharp
// Source: EF Core conventions + PostgreSQL best practices
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ✅ Explicit index on foreign key columns (PostgreSQL doesn't auto-index FKs)
    modelBuilder.Entity<Request>()
        .HasIndex(r => r.EventId)
        .HasDatabaseName("IX_Requests_EventId");

    modelBuilder.Entity<Vote>()
        .HasIndex(v => v.RequestId)
        .HasDatabaseName("IX_Votes_RequestId");

    modelBuilder.Entity<Vote>()
        .HasIndex(v => v.SessionId)
        .HasDatabaseName("IX_Votes_SessionId");

    // ✅ Composite index for common query pattern
    modelBuilder.Entity<Request>()
        .HasIndex(r => new { r.EventId, r.Status })
        .HasDatabaseName("IX_Requests_EventId_Status");
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ConfigureAwait(false) everywhere | No ConfigureAwait in ASP.NET Core apps | ASP.NET Core 1.0 (2016) | Removed SynchronizationContext—ConfigureAwait unnecessary in web apps |
| Lazy loading default | Eager loading with Include | EF Core 2.1+ | Lazy loading requires explicit opt-in via proxies |
| Manual DbContext disposal | await using pattern | C# 8.0 (2019) | Async disposal prevents blocking finalization |
| Query cache manual management | Automatic query caching | EF Core 1.0+ | EF Core caches by query tree shape automatically |
| .Result/.Wait() in controllers | async Task<IActionResult> | ASP.NET Core 1.0+ | Framework designed async-first |
| Manual connection pooling | Npgsql built-in pooling | Npgsql 3.0+ (2017) | Provider handles pooling transparently |

**Deprecated/outdated:**
- `Microsoft.AspNetCore.All` metapackage: Replaced with explicit package references in .NET 3.0+
- `ConfigureAwait(false)` in application code: Only needed in library code (ASP.NET Core has no SynchronizationContext)
- Lazy loading by default: Must explicitly enable via `.UseLazyLoadingProxies()` (disabled by default since EF Core 2.1)
- `AsNoTracking()` as opt-in: Consider setting `QueryTrackingBehavior.NoTracking` as default for read-heavy apps

## Open Questions

Things that couldn't be fully resolved:

1. **ConfigureAwaitOptions in .csproj**
   - What we know: ConfigureAwaitOptions is a new enum in .NET 9 with flags like `ContinueOnCapturedContext`, `None`, `SuppressThrowing`
   - What's unclear: Project-level .csproj setting for ConfigureAwait appears to be a proposal/discussion, not an implemented feature in .NET 9
   - Recommendation: Use AsyncFixer/Threading.Analyzers to enforce async patterns instead; revisit when .NET 10 releases if project-level ConfigureAwait settings become available

2. **Application Insights SQL query text capture**
   - What we know: Dependency tracking is enabled by default; capturing full SQL text requires configuring DependencyTrackingTelemetryModule
   - What's unclear: Exact configuration for SQL command text in modern Application Insights SDK (documentation references older APIs)
   - Recommendation: Start with default dependency tracking; add SQL text capture if needed after verifying it doesn't expose sensitive data

3. **Optimal query count budgets per endpoint**
   - What we know: Best practice is to minimize queries; 1 query per endpoint is ideal, 2-3 acceptable for complex operations
   - What's unclear: No industry-standard budgets exist; depends heavily on data model and use case
   - Recommendation: Establish budgets empirically during phase 1: measure current query counts, set targets at 50-75% reduction, enforce via integration tests

## Sources

### Primary (HIGH confidence)
- [EF Core Efficient Querying - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) - N+1 prevention, AsNoTracking, projections, Include
- [EF Core Advanced Performance Topics - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics) - DbContext pooling, compiled queries, benchmarks
- [EF Core Tracking vs No-Tracking - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/querying/tracking) - AsNoTracking vs AsNoTrackingWithIdentityResolution
- [ASP.NET Core Async Guidance - David Fowl](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md) - Anti-patterns, best practices, Task vs ValueTask
- [MiniProfiler ASP.NET Core Docs](https://miniprofiler.com/dotnet/AspDotNetCore) - Setup, configuration, EF Core integration
- [Npgsql EF Core Provider Docs](https://www.npgsql.org/efcore/?tabs=context-pooling) - Connection pooling, context pooling

### Secondary (MEDIUM confidence)
- [How to Optimize Entity Framework Core Queries - OneUpTime](https://oneuptime.com/blog/post/2026-01-28-optimize-entity-framework-core-queries/view) - 2026 guide covering N+1, AsNoTracking, projections
- [7 Entity Framework Core Optimization Techniques - Medium](https://medium.com/@cankutukoglu03/7-entity-framework-core-optimization-techniques-7c1757ed2b47) - January 2026 compilation of techniques
- [Entity Framework Core Logging - DEV Community](https://dev.to/kenakamu/entity-framework-core-logging-1hl9) - appsettings.json configuration examples
- [PostgreSQL Indexing Best Practices - MyDBOps](https://www.mydbops.com/blog/postgresql-indexing-best-practices-guide) - Foreign key indexing, composite indexes
- [Understanding ValueTask - .NET Blog](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) - When to use ValueTask vs Task

### Tertiary (LOW confidence)
- ConfigureAwaitOptions .csproj setting - Appears to be proposal/discussion, not implemented feature; treat as future possibility

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - EF Core, Npgsql, MiniProfiler are industry-standard tools with official documentation
- Architecture: HIGH - Patterns verified from official Microsoft Learn docs and maintainer guidance (David Fowl)
- Pitfalls: HIGH - Based on official diagnostics scenarios and documented anti-patterns
- ConfigureAwaitOptions: LOW - Feature appears to be discussion/proposal rather than implemented in .NET 9

**Research date:** 2026-02-05
**Valid until:** 2026-03-05 (30 days - stable ecosystem)

**Notes:**
- CrowdQR project uses ASP.NET Core 8.0 with EF Core 9.0.7 and Npgsql 9.0.4 (verified from .csproj)
- User decisions from CONTEXT.md prioritize bottom-up async migration, strict eager loading, comprehensive profiling tooling, and aggressive <50ms p95 response time targets
- All locked decisions researched deeply; Claude's discretion areas identified (query budgets, alerting thresholds, sync/async boundary handling)
