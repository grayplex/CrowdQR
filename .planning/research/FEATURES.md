# Feature Research: Performance Optimization Techniques

**Domain:** ASP.NET Core Performance Optimization with PostgreSQL and SignalR
**Researched:** 2026-02-05
**Confidence:** HIGH

## Feature Landscape

### Table Stakes (Must Have for Performance Optimization)

Performance optimizations that are expected in any production-grade ASP.NET Core application. Missing these creates obvious performance problems.

| Technique | Why Expected | Complexity | Notes |
|-----------|--------------|------------|-------|
| **Async/Await Everywhere** | Prevents thread pool starvation, industry standard for I/O operations | MEDIUM | Replace all synchronous I/O (database, HTTP, file) with async equivalents. Hot code paths must be async from controller to repository. |
| **Database Query Optimization** | N+1 queries kill performance at scale | MEDIUM | Use `.Include()` for eager loading, `.AsNoTracking()` for read-only queries, projection with `.Select()` to reduce data transfer. |
| **Connection Pooling** | Database connections are expensive to create | LOW | Configure DbContext pooling with `AddDbContextPool()`, set appropriate min/max pool sizes in connection string. |
| **Response Caching** | Reduces server load for repeated requests | LOW | HTTP response caching for GET endpoints that return stable data. Use `[ResponseCache]` attribute or middleware. |
| **Memory Cache** | In-memory caching is basic performance hygiene | LOW | Use `IMemoryCache` for frequently accessed reference data, computed results, or session data. |
| **Basic Monitoring** | Can't optimize what you can't measure | MEDIUM | Application logging with structured logs (Serilog), basic metrics tracking, error monitoring. |
| **Database Indexing** | Unindexed queries become slower as data grows | LOW | Index foreign keys, frequently queried columns, and WHERE/ORDER BY columns. Define in EF Core migrations. |
| **No Blocking Calls** | Blocking async code causes deadlocks and poor throughput | LOW | Never use `.Wait()`, `.Result`, or `Task.Run()` then await. Always use `await` properly. |
| **Pagination** | Loading all records crashes with large datasets | LOW | Implement skip/take for list endpoints, return partial results with page size and page index. |
| **HTTP Client Factory** | Prevents socket exhaustion | LOW | Use `IHttpClientFactory` instead of `new HttpClient()`. Critical for external API calls. |

### Differentiators (Advanced Optimizations)

Advanced techniques that separate high-performance applications from adequate ones. Not required for MVP but provide significant value at scale.

| Technique | Value Proposition | Complexity | Notes |
|-----------|-------------------|------------|-------|
| **Distributed Caching (Redis)** | Enables horizontal scaling with consistent cache across instances | MEDIUM | Redis with `IDistributedCache`, critical for multi-instance deployments, session sharing, and cache invalidation patterns. |
| **HybridCache (.NET 10)** | Combines in-memory and distributed caching automatically | LOW | New in .NET 10, reduces DB hits with L1 (memory) + L2 (distributed) cache strategy. Front-loads EF Core queries. |
| **SignalR Backplane (Redis)** | Enables SignalR scaling across multiple servers | HIGH | Redis backplane for SignalR allows real-time updates across load-balanced instances. Required for horizontal scale. |
| **Response Compression (Brotli)** | Reduces bandwidth by 60-80% for text responses | LOW | Brotli compression (20% better than Gzip), configure compression levels. Add `UseResponseCompression()` middleware. |
| **Rate Limiting** | Protects against abuse and ensures fair resource allocation | MEDIUM | Built-in .NET 7+ rate limiting middleware with fixed/sliding window/token bucket algorithms. Return `Retry-After` headers. |
| **Query Result Projection** | Reduces data transfer and memory allocation | LOW | Use `.Select()` to return DTOs/anonymous types with only needed columns. Can improve queries by 60-99%. |
| **Split Queries** | Prevents Cartesian explosion in complex joins | LOW | Use `.AsSplitQuery()` for queries with multiple includes. Trades 1 complex query for N simple ones. |
| **Database Compiled Queries** | Pre-compiles LINQ for reusable queries | MEDIUM | EF Core compiled queries eliminate query compilation overhead for frequently executed queries. |
| **MapStaticAssets (.NET 10)** | Optimizes static file delivery with fingerprinting and caching | LOW | New in .NET 10, optimizes static assets in APIs/MVC apps with automatic versioning and aggressive caching. |
| **Native AOT Compilation** | Reduces startup time, memory footprint, and app size | HIGH | .NET 10+ feature, significant deployment size reduction and faster cold starts. Requires compatibility analysis. |
| **OpenTelemetry Integration** | Comprehensive observability with traces, metrics, and logs | MEDIUM | Industry-standard observability, integrates with Azure Monitor/Application Insights. Provides distributed tracing. |
| **Load Testing Framework** | Validates performance under realistic load | MEDIUM | JMeter, k6, or NBomber for load testing. Measure 99th percentile latency <200ms target. Essential for capacity planning. |
| **WebSocket Compression (SignalR)** | Reduces SignalR message size over network | LOW | Enable WebSocket compression, reduce serialized object size with `[JsonIgnore]` and shortened property names. |
| **Covering Indexes (PostgreSQL)** | Enables index-only scans, improves query speed | LOW | PostgreSQL covering indexes with non-key columns. Can reduce query time from 500ms to 5ms. |
| **ArrayPool for Large Objects** | Reduces Gen 2 garbage collection pressure | MEDIUM | Pool large arrays (≥85KB) to avoid expensive GC. Use `ArrayPool<T>.Shared` for buffers in hot paths. |
| **BenchmarkDotNet Profiling** | Micro-benchmarking for algorithmic optimization | MEDIUM | Benchmark critical code paths with BenchmarkDotNet, profile with dotnet-trace. Identify CPU/memory hotspots. |

### Anti-Features (Commonly Requested, Often Problematic)

Optimization approaches that seem beneficial but create more problems than they solve.

| Technique | Why Requested | Why Problematic | Alternative |
|-----------|---------------|-----------------|-------------|
| **Aggressive Output Caching** | "Cache everything for maximum speed" | Stale data issues, cache invalidation complexity, memory bloat. Over-caching is worse than under-caching. | Cache selectively with appropriate expiration. Use cache-aside pattern with explicit invalidation for critical data. |
| **Synchronous + Task.Run Wrapper** | "Make synchronous code async by wrapping it" | Creates more thread pool pressure than synchronous code. Adds overhead without I/O benefit. | Rewrite using true async APIs (e.g., `ToListAsync()` instead of `Task.Run(() => ToList())`). Go fully async or stay synchronous. |
| **Premature Database Denormalization** | "Flatten everything to avoid joins" | Data inconsistency, complex update logic, storage bloat. Optimizes for one query pattern at expense of others. | Use proper indexing, query optimization, and materialized views. Denormalize only after measuring actual bottlenecks. |
| **Async Void Methods** | "Make event handlers async" | Crashes the process on exceptions, can't be awaited, breaks error handling. **NEVER use in ASP.NET Core.** | Use `async Task` for all async methods. For event handlers, return `Task` or use fire-and-forget pattern with explicit error handling. |
| **Global Query Filters Without AsNoTracking** | "Apply filters everywhere for security" | Unnecessary change tracking overhead for read-only queries. | Combine global query filters with `.AsNoTracking()` for reads, or use separate read/write DbContext configurations. |
| **Excessive Code Splitting (Frontend)** | "Split everything into tiny bundles" | HTTP/2+ makes many small requests less beneficial than assumed. Overhead of multiple requests can exceed bundle size savings. | Use code splitting strategically for routes/features, not every component. Bundle common dependencies together. |
| **Over-engineering Cache Invalidation** | "Build complex cache dependency tracking" | Cache invalidation is one of the hardest problems in CS. Complex systems fail in subtle ways. | Use simple time-based expiration (sliding/absolute). For critical data, use cache-aside with explicit invalidation on writes. |
| **Distributed Transactions** | "Ensure consistency across services" | Poor performance, high latency, tight coupling. Distributed locks block at scale. | Use eventual consistency with compensating transactions or Saga pattern. Design for idempotency. |
| **Aggressive ConfigureAwait(false)** | "Avoid context switching overhead" | **Not needed in ASP.NET Core** - no synchronization context. Adds noise without benefit. Legacy advice from desktop apps. | Omit `ConfigureAwait(false)` in ASP.NET Core applications. Framework is designed for async without this. |
| **Caching Database Connections** | "Reuse connections manually for speed" | Connection pooling handles this automatically. Manual connection caching causes leaks and exhaustion. | Trust the built-in connection pooling. Configure pool sizes in connection string, let ADO.NET manage lifecycle. |

## Feature Dependencies

```
Database Query Optimization
    ├──requires──> Database Indexing
    ├──requires──> Async/Await Everywhere
    └──enhances──> Connection Pooling

Response Caching
    ├──enhances──> Response Compression (Brotli)
    └──conflicts──> Real-time Updates (SignalR)

Distributed Caching (Redis)
    ├──requires──> Connection Pooling (Redis connections)
    └──enables──> SignalR Backplane

Monitoring & Observability
    ├──requires──> Basic Monitoring
    └──enhances──> Load Testing Framework

SignalR Backplane
    ├──requires──> Distributed Caching (Redis)
    └──requires──> WebSocket Compression

Load Testing Framework
    ├──requires──> Monitoring & Observability
    └──validates──> All Performance Optimizations

HybridCache (.NET 10)
    └──replaces──> Manual Memory + Distributed Cache coordination
```

### Dependency Notes

- **Database Query Optimization requires Database Indexing:** Optimized queries only work if proper indexes exist. Without indexes, query optimization techniques have minimal impact.
- **Database Query Optimization requires Async/Await Everywhere:** Async database calls only provide throughput benefits if the entire call stack is async. Blocking anywhere negates the benefit.
- **Response Caching conflicts with Real-time Updates:** Cached responses bypass controller logic, preventing SignalR from broadcasting changes. Need cache invalidation on updates.
- **SignalR Backplane requires Distributed Caching (Redis):** Redis serves as the message bus for coordinating SignalR messages across scaled-out servers.
- **HybridCache replaces manual coordination:** New .NET 10 feature eliminates need for manually coordinating IMemoryCache + IDistributedCache with L1/L2 cache strategy.
- **Load Testing validates all optimizations:** Performance improvements should be validated under realistic load before considering optimization successful.

## Implementation Roadmap

### Phase 1: Foundation (High ROI, Low Risk)

Essential optimizations with immediate impact and minimal risk.

- [x] Async/Await Everywhere — Convert all synchronous database/I/O operations
- [x] Database Query Optimization — Fix N+1 queries with Include/projection
- [x] No Blocking Calls — Audit and remove `.Wait()`, `.Result`, blocking calls
- [x] Database Indexing — Index foreign keys and frequently queried columns
- [x] Connection Pooling — Configure DbContext pooling and connection string settings
- [x] Pagination — Implement skip/take for all list endpoints
- [x] Basic Monitoring — Add structured logging and error tracking

**Trigger for completion:** All database queries use async, no N+1 queries detected, indexes on foreign keys.

### Phase 2: Caching & Compression (Medium Effort, High Impact)

Optimizations that require infrastructure but provide significant gains.

- [ ] Memory Cache — Cache reference data and computed results
- [ ] Response Caching — HTTP caching for stable GET endpoints
- [ ] Response Compression (Brotli) — Enable compression middleware
- [ ] Distributed Caching (Redis) — Setup Redis for multi-instance caching
- [ ] Query Result Projection — Use `.Select()` for DTOs instead of full entities

**Trigger for completion:** Redis deployed, caching strategy defined, compression enabled and tested.

### Phase 3: SignalR Optimization (Specialized, High Value for Real-time)

SignalR-specific optimizations for real-time performance at scale.

- [ ] WebSocket Compression — Enable SignalR message compression
- [ ] SignalR Backplane (Redis) — Scale SignalR across multiple servers
- [ ] Message Size Reduction — Optimize SignalR DTOs, use `[JsonIgnore]`
- [ ] Connection Management — Configure SignalR connection limits and timeouts

**Trigger for completion:** SignalR scales horizontally, message sizes optimized, WebSocket compression verified.

### Phase 4: Advanced Optimization (Lower Priority, Specialized Scenarios)

Advanced techniques for specific bottlenecks identified through profiling.

- [ ] Rate Limiting — Protect endpoints with built-in rate limiting middleware
- [ ] OpenTelemetry Integration — Comprehensive observability for production
- [ ] Load Testing Framework — Establish JMeter/k6 load testing pipeline
- [ ] Split Queries — Use `.AsSplitQuery()` for complex multi-include queries
- [ ] Covering Indexes — PostgreSQL covering indexes for frequent query patterns
- [ ] ArrayPool for Large Objects — Pool large buffers in hot paths (if GC profiling shows Gen 2 pressure)
- [ ] BenchmarkDotNet Profiling — Micro-benchmark critical algorithmic code

**Trigger for completion:** Observability pipeline established, load testing automated, specific bottlenecks addressed.

### Phase 5: Future Consideration (Emerging Tech, High Investment)

Techniques to evaluate for future optimization cycles.

- [ ] HybridCache (.NET 10) — Upgrade to .NET 10 and adopt HybridCache for L1/L2 caching
- [ ] MapStaticAssets (.NET 10) — Optimize static asset delivery with new .NET 10 features
- [ ] Native AOT Compilation — Reduce startup time and memory footprint (requires compatibility analysis)
- [ ] Database Compiled Queries — Pre-compile frequent LINQ queries (if profiling shows query compilation overhead)

**Trigger for evaluation:** .NET 10 adoption, profiling shows specific optimization opportunities.

## Feature Prioritization Matrix

| Technique | User Impact | Implementation Cost | Priority | Phase |
|-----------|-------------|---------------------|----------|-------|
| Async/Await Everywhere | HIGH | MEDIUM | P1 | 1 |
| Database Query Optimization | HIGH | MEDIUM | P1 | 1 |
| Database Indexing | HIGH | LOW | P1 | 1 |
| Connection Pooling | HIGH | LOW | P1 | 1 |
| No Blocking Calls | HIGH | LOW | P1 | 1 |
| Pagination | HIGH | LOW | P1 | 1 |
| Basic Monitoring | HIGH | MEDIUM | P1 | 1 |
| Memory Cache | MEDIUM | LOW | P1 | 2 |
| Response Caching | MEDIUM | LOW | P1 | 2 |
| Response Compression (Brotli) | MEDIUM | LOW | P1 | 2 |
| Distributed Caching (Redis) | MEDIUM | MEDIUM | P2 | 2 |
| Query Result Projection | MEDIUM | LOW | P2 | 2 |
| WebSocket Compression | MEDIUM | LOW | P2 | 3 |
| SignalR Backplane | MEDIUM | HIGH | P2 | 3 |
| Message Size Reduction | LOW | LOW | P2 | 3 |
| Rate Limiting | MEDIUM | MEDIUM | P2 | 4 |
| OpenTelemetry Integration | MEDIUM | MEDIUM | P2 | 4 |
| Load Testing Framework | MEDIUM | MEDIUM | P2 | 4 |
| Split Queries | LOW | LOW | P2 | 4 |
| Covering Indexes | MEDIUM | LOW | P2 | 4 |
| ArrayPool | LOW | MEDIUM | P3 | 4 |
| BenchmarkDotNet Profiling | LOW | MEDIUM | P3 | 4 |
| HybridCache | MEDIUM | LOW | P3 | 5 |
| MapStaticAssets | LOW | LOW | P3 | 5 |
| Native AOT | LOW | HIGH | P3 | 5 |
| Compiled Queries | LOW | MEDIUM | P3 | 5 |

**Priority key:**
- **P1:** Must have - addresses critical performance issues (N+1 queries, blocking I/O, missing indexes)
- **P2:** Should have - significant performance gains, enables scaling
- **P3:** Nice to have - incremental improvements, bleeding edge features

## Performance Characteristics by Optimization Type

### Database & Data Access
| Technique | Typical Impact | When to Apply |
|-----------|----------------|---------------|
| Fix N+1 with `.Include()` | 90-99% reduction in query count | Detected N+1 pattern in entity relationships |
| `.AsNoTracking()` for reads | 20-40% faster query execution | All read-only queries, DTOs, reporting |
| Projection with `.Select()` | 60-99% reduction in data transfer | Large entities, wide tables, over-fetching detected |
| Database Indexing | 500ms → 5ms query time | Slow queries on WHERE/JOIN/ORDER BY columns |
| Connection Pooling | 50-200ms per request saved | Any database access |
| `.AsSplitQuery()` | Eliminates Cartesian explosion | Multiple `.Include()` causing huge result sets |

### Caching
| Technique | Typical Impact | When to Apply |
|-----------|----------------|---------------|
| Memory Cache | 100-1000x faster than DB | Reference data, computed results, < 100MB total |
| Distributed Cache (Redis) | 10-100x faster than DB | Multi-instance deployments, session data |
| Response Caching | Near-zero latency for hits | Stable GET endpoints, public data |
| HybridCache (.NET 10) | Automatic L1/L2 strategy | .NET 10+ apps needing distributed + in-memory |

### Network & Transport
| Technique | Typical Impact | When to Apply |
|-----------|----------------|---------------|
| Response Compression (Brotli) | 60-80% bandwidth reduction | Text responses (JSON, HTML, CSS, JS) |
| WebSocket Compression (SignalR) | 40-60% message size reduction | Frequent SignalR messages, mobile clients |
| Pagination | Prevents memory exhaustion | Any endpoint returning collections |

### Async & Threading
| Technique | Typical Impact | When to Apply |
|-----------|----------------|---------------|
| Async/Await Everywhere | 5-10x more concurrent requests | Any I/O operations (DB, HTTP, files) |
| No Blocking Calls | Prevents deadlocks & starvation | Critical - audit entire codebase |
| HTTP Client Factory | Prevents socket exhaustion | Any HTTP client usage |

### Observability & Protection
| Technique | Typical Impact | When to Apply |
|-----------|----------------|---------------|
| Rate Limiting | Prevents abuse, ensures fairness | Public APIs, resource-intensive endpoints |
| OpenTelemetry | Identifies bottlenecks | Production monitoring, performance debugging |
| Load Testing | Validates capacity | Before launch, after major changes |

## Technology Stack Requirements

### Required Packages (Phase 1-2)
```xml
<!-- Database & Caching -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="10.0.0" />

<!-- Redis (for Distributed Cache & SignalR Backplane) -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.8.0" />

<!-- Compression -->
<PackageReference Include="Microsoft.AspNetCore.ResponseCompression" Version="10.0.0" />

<!-- Logging & Monitoring -->
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
```

### Advanced Packages (Phase 3-4)
```xml
<!-- SignalR Backplane -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="10.0.0" />

<!-- Observability -->
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.10.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.10.0" />

<!-- Rate Limiting (built-in .NET 7+) -->
<!-- No additional package needed -->

<!-- Profiling & Benchmarking -->
<PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
```

### Load Testing Tools (External)
- **Apache JMeter** - Popular, feature-rich, GUI-based load testing
- **k6** - Modern, scripting-based, easy CI/CD integration
- **NBomber** - .NET-native, C#/F# test scenarios
- **dotnet-counters** - Live metrics monitoring
- **dotnet-trace** - Profiling and diagnostics

## Expected Performance Improvements

Based on industry benchmarks and Microsoft documentation:

| Optimization Area | Before | After | Improvement |
|-------------------|--------|-------|-------------|
| N+1 Query Fix | 101 queries for 100 items | 1-2 queries | 98% reduction |
| Async Conversion | 200 concurrent requests | 1000-2000 concurrent | 5-10x throughput |
| Response Caching (hit) | 50-100ms database query | <1ms cache hit | 99% latency reduction |
| Brotli Compression | 500KB JSON response | 100-150KB compressed | 70-80% bandwidth |
| Database Indexing | 500ms query | 5ms query | 99% query time |
| Projection (Select) | 100KB entity data | 10KB DTO | 90% data transfer |
| Connection Pooling | 50-200ms connection overhead | <1ms pooled connection | 99% reduction |
| SignalR WebSocket Compression | 10KB message | 4-6KB compressed | 40-60% reduction |

**Target Performance Goals:**
- **API Latency:** 99th percentile <200ms
- **Database Queries:** <50ms for 95% of queries
- **Concurrent Users:** 1000+ simultaneous connections
- **SignalR Messages:** <100ms end-to-end latency
- **Cache Hit Rate:** >80% for cacheable endpoints
- **Error Rate:** <0.1% under normal load

## Sources

### Official Microsoft Documentation
- [ASP.NET Core Best Practices | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0)
- [Distributed caching in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0)
- [Response compression in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0)
- [Rate limiting middleware in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Efficient Querying - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [ASP.NET Core load/stress testing | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/test/load-tests?view=aspnetcore-10.0)

### 2026 Best Practices
- [Performance Tuning in ASP.NET Core: Best Practices for 2026 | Syncfusion Blogs](https://www.syncfusion.com/blogs/post/performance-tuning-in-aspnetcore-2026)
- [ASP.NET Core Performance Best Practices | BizTechCS](https://www.biztechcs.com/blog/asp-net-core-performance-best-practices/)

### Entity Framework Core & PostgreSQL
- [How to Use Entity Framework Core with PostgreSQL](https://oneuptime.com/blog/post/2026-01-26-entity-framework-core-postgresql/view)
- [Avoiding N+1 Queries in EF Core: Practical Patterns and Fixes | Medium](https://medium.com/@kittikawin_ball/avoiding-n-1-queries-in-ef-core-practical-patterns-and-fixes-9ef8da6a6a9f)
- [7 Entity Framework Core Optimization Techniques | Medium](https://medium.com/@cankutukoglu03/7-entity-framework-core-optimization-techniques-7c1757ed2b47)
- [Npgsql Entity Framework Core Provider | Npgsql Documentation](https://www.npgsql.org/efcore/)
- [Indexes | Npgsql Documentation](https://www.npgsql.org/efcore/modeling/indexes.html)

### SignalR Performance
- [SignalR Performance | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/signalr/overview/performance/signalr-performance)
- [Advanced SignalR Techniques in .NET: Scalability, Performance and Custom Protocols - NashTech Blog](https://blog.nashtechglobal.com/advanced-signalr-techniques-in-net-scalability-performance-and-custom-protocols/)

### Distributed Caching & Redis
- [Distributed Caching in ASP.NET Core 10 with Redis | Medium](https://medium.com/codetodeploy/distributed-caching-in-asp-net-core-10-with-redis-2f0c4a837c23)
- [Distributed Caching with Redis and Response Caching in ASP.NET Core 8 | Medium](https://medium.com/@cizu64/distributed-caching-with-redis-and-response-caching-in-asp-net-core-8-169d7c6a7d3b)

### Async/Await Best Practices
- [AspNetCoreDiagnosticScenarios/AsyncGuidance.md | GitHub](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)
- [Async/Await - Best Practices in Asynchronous Programming | Microsoft Learn](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)

### Observability & Monitoring
- [.NET Observability with OpenTelemetry - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
- [Example: Use OpenTelemetry with Azure Monitor and Application Insights | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights)

### Anti-Patterns & Common Mistakes
- [Top 10 .NET Performance Anti-Patterns You Should Fix Today | Medium](https://medium.com/turbo-net/top-10-net-performance-anti-patterns-you-should-fix-today-d58f4a682340)
- [Common .NET Core Anti-Patterns and How to Avoid Them | Medium](https://medium.com/@robhutton8/common-net-core-anti-patterns-and-how-to-avoid-them-533b9812b6d5)
- [Performance testing and antipatterns - Azure Architecture Center | Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/antipatterns/)

### Load Testing & Profiling
- [NBomber - Distributed load testing framework for .NET](https://nbomber.com/)
- [Performance Testing of ASP.NET Core APIs With k6 - Code Maze](https://code-maze.com/aspnetcore-performance-testing-with-k6/)
- [BenchmarkDotNet - Powerful .NET library for benchmarking | GitHub](https://github.com/dotnet/BenchmarkDotNet)

---
*Feature research for: CrowdQR Performance Optimization Milestone*
*Researched: 2026-02-05*
*Confidence: HIGH - Based on official Microsoft documentation, .NET 10 features, and current 2026 best practices*
