# Architecture Research: Performance Optimization Patterns

**Domain:** Performance optimization for existing ASP.NET Core + EF Core + SignalR application
**Researched:** 2026-02-05
**Confidence:** HIGH

## Executive Summary

Performance optimizations for ASP.NET Core applications integrate at multiple architectural layers. This research maps where caching, monitoring, query optimization, and async patterns fit into CrowdQR's existing architecture, providing clear integration points and build order recommendations.

**Key Finding:** Performance optimizations should follow dependency order - async conversion first (foundation), then query optimization (data layer), then caching (application layer), then monitoring (cross-cutting).

---

## Current Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Web Frontend (Razor Pages)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ Razor Pages  │  │ Web Services │  │ SignalR      │           │
│  │              │  │ (API clients)│  │ Client       │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
│         │                  │                  │                   │
│         └──────────────────┴──────────────────┘                   │
│                            │                                      │
│                            │ HTTP / WebSocket                     │
│                            ↓                                      │
├─────────────────────────────────────────────────────────────────┤
│                      API Layer (ASP.NET Core)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ Controllers  │  │ Middleware   │  │ SignalR Hubs │           │
│  │              │  │              │  │              │           │
│  └──────┬───────┘  └──────────────┘  └──────┬───────┘           │
│         │                                     │                   │
├─────────┴─────────────────────────────────────┴─────────────────┤
│                     Service Layer                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ AuthService  │  │ HubNotif     │  │ Email        │           │
│  │ TokenService │  │ Service      │  │ Service      │           │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘           │
│         │                  │                  │                   │
├─────────┴──────────────────┴──────────────────┴─────────────────┤
│                     Data Access Layer                            │
│  ┌─────────────────────────────────────────────────────┐         │
│  │              CrowdQRContext (EF Core)               │         │
│  │  DbSet<User> | DbSet<Event> | DbSet<Request>       │         │
│  └─────────────────────┬───────────────────────────────┘         │
│                        │                                         │
├────────────────────────┴─────────────────────────────────────────┤
│                    PostgreSQL Database                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │ users    │  │ events   │  │ requests │  │ votes    │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Performance Optimization Integration Points

### 1. Async Patterns (Foundation Layer)

**Where:** Controllers, Services, Data Access (all layers)
**Current State:** Partial async implementation (controllers use `Task<>` but may not be fully async)
**Integration:** Refactor to ensure entire call stack is async

#### Integration Points

```
Controller Layer:
  EventController.GetEvents() → async Task<ActionResult>
  ├─ Ensure all actions return Task<T>
  ├─ Use async I/O operations (ReadFormAsync, etc.)
  └─ Never use async void (except event handlers)

Service Layer:
  AuthService.AuthenticateUser() → async Task<AuthResultDto>
  ├─ All database calls async
  ├─ All HTTP calls async
  └─ Background tasks use IServiceScopeFactory

Data Access Layer:
  CrowdQRContext queries → ToListAsync(), FirstOrDefaultAsync()
  ├─ Replace .ToList() with .ToListAsync()
  ├─ Replace .FirstOrDefault() with .FirstOrDefaultAsync()
  ├─ Replace .SingleOrDefault() with .SingleOrDefaultAsync()
  └─ Replace .Count() with .CountAsync()
```

**Architecture Pattern:**
```csharp
// CURRENT (likely synchronous in places)
public ActionResult<Event> GetEvent(int id)
{
    var evt = _context.Events.FirstOrDefault(e => e.EventId == id);
    return Ok(evt);
}

// OPTIMIZED (fully async)
public async Task<ActionResult<Event>> GetEvent(int id)
{
    var evt = await _context.Events
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.EventId == id);
    return Ok(evt);
}
```

**Build Order Impact:** Phase 1 - Foundation for all other optimizations

---

### 2. Caching Layer

**Where:** Between Service Layer and Data Access Layer (preferred) OR in Service Layer
**Current State:** No caching implementation
**Integration:** Add IDistributedCache abstraction

#### Architecture Options

**Option A: Service-Level Caching (Recommended)**

```
Controller → Service (with cache) → DbContext
              ↓
         IDistributedCache
```

```csharp
public class EventService
{
    private readonly CrowdQRContext _context;
    private readonly IDistributedCache _cache;

    public async Task<Event?> GetEventBySlugAsync(string slug)
    {
        // Try cache first
        var cacheKey = $"event:slug:{slug}";
        var cachedJson = await _cache.GetStringAsync(cacheKey);

        if (cachedJson != null)
        {
            return JsonSerializer.Deserialize<Event>(cachedJson);
        }

        // Cache miss - query database
        var evt = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Slug == slug);

        if (evt != null)
        {
            // Store in cache
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(evt),
                options
            );
        }

        return evt;
    }
}
```

**Option B: Repository Pattern with Caching**

```
Controller → Service → CachedRepository → DbContext
                            ↓
                      IDistributedCache
```

More complex, better separation of concerns for larger apps.

#### Cache Integration Strategy

| Component | Cache Type | Purpose | TTL |
|-----------|-----------|---------|-----|
| Event lookups (by slug) | Distributed | Frequent reads, rarely changes | 30 min (sliding 5 min) |
| Active session info | Distributed | Shared across web/api instances | 10 min (absolute) |
| User authentication | Memory + Distributed | High frequency, security sensitive | 15 min (sliding 3 min) |
| Dashboard aggregates | Distributed | Expensive queries | 5 min (absolute) |
| Request vote counts | Distributed | Updated via SignalR invalidation | 2 min (absolute) |

**Cache Invalidation Points:**

```csharp
// When event updated, invalidate cache
public async Task UpdateEventAsync(Event evt)
{
    await _context.SaveChangesAsync();

    // Invalidate cache entries
    await _cache.RemoveAsync($"event:slug:{evt.Slug}");
    await _cache.RemoveAsync($"event:id:{evt.EventId}");

    // Notify via SignalR if needed
    await _hubContext.Clients.Group($"event-{evt.EventId}")
        .SendAsync("EventUpdated", evt);
}
```

**Build Order Impact:** Phase 3 - After async and query optimization

---

### 3. Database Query Optimization (Data Layer)

**Where:** CrowdQRContext and service layer query construction
**Current State:** Direct EF Core queries, some with Include but likely not optimized
**Integration:** Apply EF Core performance patterns

#### Optimization Patterns by Query Type

**Pattern 1: Eager Loading with Filtering**

```csharp
// CURRENT (from EventController.GetEvents)
var events = await _context.Events
    .Include(e => e.DJ)  // Loads all DJ data
    .ToListAsync();

// OPTIMIZED
var events = await _context.Events
    .Include(e => e.DJ)
    .Where(e => e.IsActive)  // Filter early
    .Select(e => new EventDto  // Projection - only needed fields
    {
        EventId = e.EventId,
        Name = e.Name,
        Slug = e.Slug,
        CreatedAt = e.CreatedAt,
        IsActive = e.IsActive,
        DjUsername = e.DJ.Username,  // Only needed DJ field
        DjUserId = e.DJ.UserId
    })
    .AsNoTracking()  // Read-only = 29% faster
    .ToListAsync();
```

**Pattern 2: Split Queries for Collections**

```csharp
// When loading event with many requests/votes
var eventWithRequests = await _context.Events
    .AsSplitQuery()  // Prevents cartesian explosion
    .Include(e => e.Requests)
        .ThenInclude(r => r.Votes)
    .FirstOrDefaultAsync(e => e.EventId == id);
```

**Pattern 3: No-Tracking for Read-Only**

```csharp
// Dashboard queries (read-only aggregates)
var stats = await _context.Requests
    .AsNoTracking()  // Faster, less memory
    .Where(r => r.EventId == eventId)
    .GroupBy(r => r.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();
```

**Pattern 4: Compiled Queries for Hot Paths**

```csharp
// For frequently called queries (authentication, session lookup)
private static readonly Func<CrowdQRContext, string, Task<User?>>
    GetUserByUsername = EF.CompileAsyncQuery(
        (CrowdQRContext context, string username) =>
            context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Username == username)
    );

// Usage in service
var user = await GetUserByUsername(_context, username);
```

**Pattern 5: Streaming Large Results**

```csharp
// For reports or exports
public async IAsyncEnumerable<RequestDto> GetAllRequestsStreamAsync(int eventId)
{
    await foreach (var request in _context.Requests
        .AsNoTracking()
        .Where(r => r.EventId == eventId)
        .OrderBy(r => r.CreatedAt)
        .AsAsyncEnumerable())
    {
        yield return MapToDto(request);
    }
}
```

#### Database Indexes for PostgreSQL

**Recommended Indexes:**

```sql
-- Slug lookups (events, sessions)
CREATE INDEX idx_events_slug ON events(slug);
CREATE INDEX idx_sessions_code ON sessions(code);

-- Foreign key lookups
CREATE INDEX idx_requests_event_id ON requests(event_id);
CREATE INDEX idx_requests_session_id ON requests(session_id);
CREATE INDEX idx_votes_request_id ON votes(request_id);
CREATE INDEX idx_votes_user_id ON votes(user_id);

-- Composite indexes for common queries
CREATE INDEX idx_requests_event_status ON requests(event_id, status);
CREATE INDEX idx_sessions_event_active ON sessions(event_id, is_active);

-- Covering index for dashboard queries (PostgreSQL specific)
CREATE INDEX idx_requests_covering
ON requests(event_id, status)
INCLUDE (created_at, song_name);
```

**In EF Core Migrations:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Event>(entity =>
    {
        entity.HasIndex(e => e.Slug).IsUnique();
    });

    modelBuilder.Entity<Request>(entity =>
    {
        entity.HasIndex(r => new { r.EventId, r.Status })
              .HasDatabaseName("idx_requests_event_status");
    });
}
```

**Build Order Impact:** Phase 2 - After async, before caching

---

### 4. SignalR Performance Optimization

**Where:** Hubs, HubNotificationService
**Current State:** Single-server SignalR with WebSocket support
**Integration:** Message batching, backplane preparation, connection optimization

#### Optimization Patterns

**Pattern 1: Message Batching**

```csharp
public class HubNotificationService
{
    private readonly IHubContext<CrowdQRHub> _hubContext;
    private readonly Channel<HubMessage> _messageQueue;

    public HubNotificationService(IHubContext<CrowdQRHub> hubContext)
    {
        _hubContext = hubContext;
        _messageQueue = Channel.CreateUnbounded<HubMessage>();

        // Background task to batch and send
        _ = Task.Run(async () => await ProcessMessageBatchesAsync());
    }

    private async Task ProcessMessageBatchesAsync()
    {
        var batch = new List<HubMessage>();

        while (await _messageQueue.Reader.WaitToReadAsync())
        {
            // Collect messages for 100ms
            var deadline = DateTime.UtcNow.AddMilliseconds(100);

            while (DateTime.UtcNow < deadline &&
                   _messageQueue.Reader.TryRead(out var message))
            {
                batch.Add(message);
            }

            if (batch.Any())
            {
                // Send batched update
                await _hubContext.Clients.Group(batch[0].GroupId)
                    .SendAsync("BatchUpdate", batch);
                batch.Clear();
            }
        }
    }
}
```

**Pattern 2: Differential Updates (Not Full State)**

```csharp
// Instead of sending entire request list
await Clients.Group(eventId).SendAsync("RequestListUpdated", allRequests);

// Send only the change
await Clients.Group(eventId).SendAsync("RequestAdded", newRequest);
await Clients.Group(eventId).SendAsync("RequestStatusChanged",
    new { RequestId = id, NewStatus = status });
```

**Pattern 3: Backplane Preparation (Future Scaling)**

```csharp
// When scaling to multiple servers, add Redis backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("CrowdQR");
    });
```

**Pattern 4: Connection Filtering**

```csharp
// In CrowdQRHub
public override async Task OnConnectedAsync()
{
    var eventId = Context.GetHttpContext()?.Request.Query["eventId"];

    if (!string.IsNullOrEmpty(eventId))
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
    }

    await base.OnConnectedAsync();
}
```

**Build Order Impact:** Phase 4 - After core optimizations proven

---

### 5. Monitoring & Instrumentation (Cross-Cutting)

**Where:** All layers via OpenTelemetry instrumentation
**Current State:** Health checks configured, no APM
**Integration:** OpenTelemetry + Application Insights for observability

#### Architecture Integration

```
┌─────────────────────────────────────────────────────────────────┐
│                   OpenTelemetry Instrumentation                  │
│  (Automatic instrumentation for ASP.NET, EF Core, HttpClient)   │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Controllers → Services → DbContext → PostgreSQL                 │
│      │            │           │                                   │
│      ├─ Traces ───┼─ Traces ─┼─ Traces (SQL queries)            │
│      ├─ Metrics ──┼─ Metrics ─┼─ Metrics (query duration)        │
│      └─ Logs ─────┴─ Logs ────┴─ Logs (exceptions)               │
│                                                                   │
│                            ↓                                      │
│                  Azure Monitor Exporter                           │
│                            ↓                                      │
│                  Application Insights                             │
└─────────────────────────────────────────────────────────────────┘
```

#### Implementation

```csharp
// Program.cs - API
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration
            .GetConnectionString("ApplicationInsights");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = (httpContext) =>
                {
                    // Don't trace health checks
                    return !httpContext.Request.Path.StartsWithSegments("/health");
                };
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
            })
            .AddHttpClientInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    });
```

**Custom Metrics for CrowdQR:**

```csharp
// Track business metrics
public class CrowdQRMetrics
{
    private readonly Meter _meter;
    private readonly Counter<int> _requestsCreated;
    private readonly Counter<int> _votesRecorded;
    private readonly Histogram<double> _requestProcessingTime;

    public CrowdQRMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("CrowdQR.Api");

        _requestsCreated = _meter.CreateCounter<int>(
            "crowdqr.requests.created",
            description: "Number of song requests created"
        );

        _votesRecorded = _meter.CreateCounter<int>(
            "crowdqr.votes.recorded",
            description: "Number of votes recorded"
        );

        _requestProcessingTime = _meter.CreateHistogram<double>(
            "crowdqr.request.processing_time",
            unit: "ms",
            description: "Time to process request submission"
        );
    }

    public void RecordRequestCreated(string eventId) =>
        _requestsCreated.Add(1, new KeyValuePair<string, object?>("event.id", eventId));

    public void RecordVote(string eventId) =>
        _votesRecorded.Add(1, new KeyValuePair<string, object?>("event.id", eventId));

    public void RecordProcessingTime(double milliseconds, string operation) =>
        _requestProcessingTime.Record(milliseconds,
            new KeyValuePair<string, object?>("operation", operation));
}
```

**Build Order Impact:** Phase 5 - Last, after optimizations to measure impact

---

## Recommended Build Order

Based on dependencies and incremental value:

### Phase 1: Async Refactoring (Foundation)
**Why First:** Required for all other optimizations to work correctly
**Impact:** Prevents thread pool starvation, enables scalability
**Effort:** Medium (refactor existing code)

**Tasks:**
1. Audit all controllers for sync methods
2. Convert to async Task<> return types
3. Ensure all DbContext calls use async methods
4. Update service layer to be fully async
5. Test for deadlocks and blocking

**Validation:** No `.Result` or `.Wait()` calls, all I/O is async

---

### Phase 2: Query Optimization (Data Layer)
**Why Second:** Biggest performance wins, enables effective caching
**Impact:** Reduces database load, faster response times
**Effort:** Medium-High (requires SQL analysis)

**Tasks:**
1. Add database indexes (migrations)
2. Add AsNoTracking to read-only queries
3. Apply projections (Select) to reduce data transfer
4. Identify N+1 queries and add eager loading
5. Use split queries for large collections
6. Add compiled queries for hot paths

**Validation:** Query profiling shows reduced round trips and execution time

---

### Phase 3: Caching Layer
**Why Third:** Builds on query optimizations, requires async
**Impact:** Reduces database calls by 60-80% for cacheable data
**Effort:** Medium (new infrastructure)

**Tasks:**
1. Add IDistributedCache dependency injection
2. Add Redis for distributed cache (production)
3. Implement cache-aside pattern in services
4. Add cache invalidation logic
5. Configure cache expiration policies
6. Add cache hit/miss metrics

**Validation:** Cache hit rate >70% for read-heavy endpoints

---

### Phase 4: SignalR Optimization
**Why Fourth:** Requires async foundation, less critical than data layer
**Impact:** Reduces bandwidth, prepares for scaling
**Effort:** Low-Medium (optimization of existing)

**Tasks:**
1. Implement message batching
2. Switch to differential updates
3. Add connection pooling configuration
4. Prepare backplane infrastructure (Redis)
5. Add SignalR-specific monitoring

**Validation:** Reduced message size, stable under 1000+ connections

---

### Phase 5: Monitoring & Observability
**Why Last:** Measures impact of all previous optimizations
**Impact:** Visibility into performance, ongoing optimization
**Effort:** Low-Medium (mostly configuration)

**Tasks:**
1. Add OpenTelemetry instrumentation
2. Configure Application Insights
3. Add custom business metrics
4. Create dashboards for key metrics
5. Set up alerts for performance degradation
6. Document baseline vs optimized metrics

**Validation:** Can identify bottlenecks and track improvements

---

## Scaling Considerations

| User Load | Architecture Adjustments |
|-----------|--------------------------|
| **0-1k users** | Single server, in-memory cache acceptable, current architecture sufficient |
| **1k-10k users** | Add Redis distributed cache, optimize database queries, implement connection pooling |
| **10k-50k users** | Add SignalR Redis backplane, scale API horizontally (multiple instances), read replicas for database |
| **50k+ users** | CDN for static assets, database sharding by event, dedicated SignalR service (Azure SignalR Service) |

**First bottleneck:** Database queries without indexes (N+1 problems)
**Second bottleneck:** Single-server SignalR message broadcasting
**Third bottleneck:** Database write contention on votes table

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Sync-over-Async

**What people do:**
```csharp
var result = someAsyncMethod().Result;  // Blocks thread
```

**Why it's wrong:** Causes thread pool starvation, negates async benefits, can cause deadlocks

**Do this instead:**
```csharp
var result = await someAsyncMethod();
```

### Anti-Pattern 2: Caching Without Invalidation

**What people do:** Set long cache TTLs and never invalidate when data changes

**Why it's wrong:** Stale data shown to users, especially bad for real-time apps like CrowdQR

**Do this instead:** Implement cache invalidation on write operations and reasonable TTLs

### Anti-Pattern 3: Loading Full Entities for Read-Only

**What people do:**
```csharp
var events = await _context.Events.Include(e => e.DJ).ToListAsync();
// Returns to API as-is
```

**Why it's wrong:** Loads unnecessary columns, enables change tracking overhead, sends excess data

**Do this instead:**
```csharp
var events = await _context.Events
    .AsNoTracking()
    .Select(e => new EventDto { /* only needed fields */ })
    .ToListAsync();
```

### Anti-Pattern 4: Broadcasting Full State via SignalR

**What people do:** Send entire request list on every vote change

**Why it's wrong:** Wastes bandwidth, increases latency, overwhelms clients

**Do this instead:** Send only the delta (what changed)

### Anti-Pattern 5: N+1 Query Pattern

**What people do:**
```csharp
var events = await _context.Events.ToListAsync();
foreach (var evt in events)
{
    var requestCount = await _context.Requests
        .CountAsync(r => r.EventId == evt.EventId);  // N queries!
}
```

**Why it's wrong:** Generates 1 query for events + N queries for each event's requests

**Do this instead:**
```csharp
var eventStats = await _context.Events
    .Select(e => new
    {
        Event = e,
        RequestCount = e.Requests.Count()
    })
    .ToListAsync();  // Single query with JOIN
```

---

## Integration Points Summary

| Optimization | Layer | Integration Point | New Components |
|-------------|-------|-------------------|----------------|
| **Async** | All | Controllers, Services, DbContext | None (refactor existing) |
| **Query Optimization** | Data | EF Core queries in services | Database indexes, compiled queries |
| **Caching** | Service | Between service and data layer | IDistributedCache, Redis (prod) |
| **SignalR** | Hub | HubNotificationService, CrowdQRHub | Message batching, Redis backplane (future) |
| **Monitoring** | Cross-cutting | Middleware, DI configuration | OpenTelemetry, Application Insights |

---

## Sources

**Official Microsoft Documentation (HIGH confidence):**
- [Distributed caching in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)
- [Efficient Querying - EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Npgsql Indexes Documentation](https://www.npgsql.org/efcore/modeling/indexes.html)
- [Enable OpenTelemetry in Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)

**Performance Optimization Resources (MEDIUM-HIGH confidence):**
- [Performance Tuning in ASP.NET Core: Best Practices for 2026](https://www.syncfusion.com/blogs/post/performance-tuning-in-aspnetcore-2026)
- [How to Optimize Entity Framework Core Queries (2026)](https://oneuptime.com/blog/post/2026-01-28-optimize-entity-framework-core-queries/view)
- [ASP.NET Core Developer Roadmap 2026](https://logiclense.com/product/asp-net-core-developer-roadmap-2026/)

**Async Patterns (HIGH confidence):**
- [AspNetCoreDiagnosticScenarios - Async Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)

**SignalR Scaling (MEDIUM-HIGH confidence):**
- [SignalR Performance - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/signalr/overview/performance/signalr-performance)
- [Advanced SignalR Techniques in .NET](https://blog.nashtechglobal.com/advanced-signalr-techniques-in-net-scalability-performance-and-custom-protocols/)
- [Scaling SignalR Applications](https://ably.com/topic/scaling-signalr)

**Caching Architecture (HIGH confidence):**
- [Distributed Caching in ASP.NET Core 10 with Redis](https://medium.com/codetodeploy/distributed-caching-in-asp-net-core-10-with-redis-2f0c4a837c23)
- [In-Memory Caching in ASP.NET Core](https://codewithmukesh.com/blog/in-memory-caching-in-aspnet-core/)

---

*Architecture research for: CrowdQR Performance Optimization*
*Researched: 2026-02-05*
