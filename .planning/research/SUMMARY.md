# Project Research Summary

**Project:** CrowdQR Performance Optimization
**Domain:** Performance optimization for existing ASP.NET Core application
**Researched:** 2026-02-05
**Confidence:** HIGH

## Executive Summary

CrowdQR is a mature ASP.NET Core 8.0 application with PostgreSQL and SignalR that needs a subsequent performance optimization milestone. The recommended approach follows a foundation-first strategy: establish async patterns and query optimization before adding infrastructure like Redis caching and OpenTelemetry monitoring. This dependency ordering prevents common pitfalls like cache stampedes and connection pool exhaustion that plague rushed performance projects.

The research reveals that the biggest wins come from fixing fundamentals (N+1 queries, blocking async calls, missing indexes) rather than adding new technology. Expert consensus suggests a 5-phase approach: Foundation (async + query basics), Caching (Redis distributed cache), SignalR Optimization (backplane + compression), Advanced Optimization (observability + profiling), and Future Tech (HybridCache/.NET 10 features). This ordering ensures each phase builds on validated improvements from the previous phase.

Key risk: Over-optimization without measurement. The research consistently warns against premature optimization and cargo cult practices. Establish performance budgets (p95 <200ms for API calls), profile with real data (100+ requests per event), and validate improvements with load testing. CrowdQR's real-time nature means cache invalidation bugs are especially dangerous—users will immediately notice stale vote counts, so invalidation logic must ship with caching from day one.

## Key Findings

### Recommended Stack

Performance optimizations integrate at multiple architectural layers with Redis as the centerpiece for production deployment. The stack emphasizes vendor-agnostic observability through OpenTelemetry rather than locking into Azure Application Insights or Datadog proprietary SDKs.

**Core technologies:**
- **Redis (StackExchangeRedis 8.0.23)**: Distributed cache and SignalR backplane for multi-server deployments. Essential for production scale-out. Alternative: in-memory cache for single-server development.
- **OpenTelemetry (1.15.0)**: Industry-standard observability for traces, metrics, and logs. Vendor-agnostic, works with any APM backend. Automatic instrumentation for ASP.NET Core, EF Core, and HTTP clients.
- **k6 or JMeter**: Modern load testing tools to validate optimizations under realistic load. k6 preferred for CI/CD integration, JMeter for complex GUI-based scenarios.
- **BenchmarkDotNet (0.15.8)**: Micro-benchmarking for algorithmic optimizations. Provides statistical rigor to avoid benchmarking mistakes.
- **MiniProfiler (4.5.4)**: Development-time profiling UI for identifying N+1 queries and slow operations. Essential for finding optimization targets before production deployment.

**Critical version notes:**
- ASP.NET Core 8.0 does NOT have HybridCache (that's .NET 9+). Use manual IDistributedCache + IMemoryCache coordination.
- Output Caching (ASP.NET Core 7+) is superior to deprecated Response Caching middleware.
- All packages version-aligned to 8.0.x for ASP.NET Core 8.0 compatibility.

### Expected Features

Performance optimization is not about new features but enhancing existing functionality with better response times, higher throughput, and horizontal scalability. Features are grouped by implementation priority based on ROI and risk.

**Must have (table stakes):**
- Async/await throughout call stack — prevents thread pool starvation, enables 5-10x concurrency improvement
- Database query optimization — fix N+1 queries with Include/projection, add AsNoTracking for 20-40% speedup
- Database indexing — foreign keys and frequently queried columns, can improve queries from 500ms to 5ms
- Connection pooling — configure DbContext pooling to eliminate 50-200ms connection overhead
- Pagination — prevent memory exhaustion on large result sets
- Basic monitoring — structured logging to identify bottlenecks

**Should have (competitive):**
- Distributed caching (Redis) — enables horizontal scaling, 10-100x faster than database queries
- Response compression (Brotli) — 60-80% bandwidth reduction for JSON responses
- SignalR backplane (Redis) — required for multi-server real-time updates
- OpenTelemetry integration — comprehensive observability for production debugging
- Load testing framework — validates optimizations under realistic load (99th percentile <200ms target)
- Split queries (AsSplitQuery) — prevents Cartesian explosion for complex includes

**Defer (v2+):**
- HybridCache — requires .NET 9+ upgrade, not available for .NET 8
- Native AOT compilation — high investment, requires compatibility analysis
- ArrayPool for large objects — only needed if profiling shows Gen 2 GC pressure
- Compiled queries — only if profiling shows query compilation overhead

**Anti-features (never implement):**
- Response Caching middleware — replaced by Output Caching in ASP.NET Core 7+
- Task.Run wrapping synchronous code — creates more overhead than benefit
- Lazy loading in EF Core — causes silent N+1 query disasters
- ConfigureAwait(false) in ASP.NET Core — no-op since framework removed synchronization context
- Aggressive output caching without invalidation — stale data is worse than slow data in real-time apps

### Architecture Approach

Performance optimizations integrate at five layers: async patterns (foundation across all layers), caching (between service and data layers), query optimization (data access layer), SignalR enhancements (hub layer), and monitoring (cross-cutting). Service-level caching is recommended over repository pattern for CrowdQR's complexity level.

**Major components:**
1. **Async Foundation** — refactor controllers, services, and DbContext to use Task<T> throughout. Replace all ToList() with ToListAsync(), FirstOrDefault() with FirstOrDefaultAsync(). Audit for blocking calls (.Result, .Wait()). Foundation for all other optimizations.
2. **Caching Layer** — IDistributedCache abstraction in service layer with Redis backing for production. Implement cache-aside pattern with explicit invalidation on writes. Critical: cache invalidation must ship with caching, not added later.
3. **Query Optimization** — apply AsNoTracking to read-only queries, use Select() projection for DTOs, add Include() for eager loading, use AsSplitQuery() for multi-collection includes. Add database indexes via EF Core migrations.
4. **SignalR Optimization** — message batching, differential updates (send only changes, not full state), Redis backplane preparation for multi-server deployments. WebSocket compression for bandwidth reduction.
5. **Observability** — OpenTelemetry instrumentation with sampling (10% default, 100% for errors). Custom metrics for business events (requests created, votes recorded). Application Insights or vendor-agnostic OTLP exporter.

**Key patterns:**
- Cache invalidation service centralizes cache clearing logic across multiple keys
- Tag-based invalidation allows removing related cache entries together
- Compiled queries for high-frequency hot paths (authentication, session lookup)
- IDbContextFactory for background tasks to prevent scoped context disposal issues

### Critical Pitfalls

Performance optimization projects fail in predictable ways. These are the top pitfalls with prevention strategies.

1. **Async-over-Sync Conversion Without Understanding** — Blindly replacing .Result with await without ensuring async all the way causes deadlocks and thread pool starvation. Prevention: Go async throughout entire call stack, never use async void, don't wrap sync in Task.Run. Audit all controllers and services for blocking calls before considering optimization complete.

2. **Cache Stampede (Thundering Herd)** — When popular cache entry expires, hundreds of concurrent requests all trigger the expensive database query simultaneously, causing worse performance than no caching. Prevention: Use HybridCache (if .NET 9+) with built-in stampede protection, or implement manual locking with SemaphoreSlim. Add staggered TTLs. Test with 100 concurrent requests on cache miss.

3. **N+1 Queries Persist After "Optimization"** — Developers add Include() but N+1 problems persist due to implicit loading in loops, projection after materialization, or missing includes in related services. Prevention: Enable query logging in development, use AsNoTracking for reads, create integration tests that count queries (should be ≤3 per request), test with realistic data volumes (100+ requests per event).

4. **Cache Invalidation Bugs Cause Stale Data** — Cached data becomes stale after updates because invalidation logic was not implemented alongside caching. Critical for CrowdQR's real-time nature—users immediately notice stale vote counts. Prevention: Invalidate cache immediately after writes, create centralized invalidation service, combine cache with SignalR for real-time updates, use short TTLs for frequently changing data.

5. **Over-Optimization Creates Unmaintainable Code** — Complex micro-optimizations based on assumptions rather than profiling offer <5% improvement but kill team velocity. Prevention: Profile first with dotnet-trace, establish performance budgets (p95 <200ms), only optimize hot paths (executed thousands of times), use BenchmarkDotNet to validate improvements ≥20%, reject optimizations without measurements.

6. **APM Overhead Degrades Performance** — Full instrumentation causes 100-500ms latency increase and 20-40% CPU overhead. Prevention: Use sampling (10% default, 100% for errors), disable expensive instrumentation (SQL statement capture), use metrics instead of traces for performance monitoring, load test WITH APM enabled to measure overhead.

7. **Database Connection Pool Exhaustion After Async** — Async conversion makes it easier to leak connections through missing using statements or holding connections during slow external calls. Prevention: Always use using/await using with DbContext, configure MaxPoolSize explicitly (100), use IDbContextFactory for background tasks, don't hold connections during external API calls.

## Implications for Roadmap

Based on research, suggested phase structure follows dependency order: async foundation → query optimization → caching → SignalR → monitoring. Each phase builds on validated improvements from previous phases.

### Phase 1: Foundation (Async + Query Basics)
**Rationale:** Async patterns and query fundamentals must be correct before adding infrastructure. Caching broken queries doesn't help. Thread pool starvation undermines all other optimizations.
**Delivers:** Fully async call stack, no blocking calls, N+1 queries eliminated, database indexes on foreign keys, pagination on list endpoints
**Addresses:** Async/await everywhere, database query optimization, no blocking calls, database indexing, connection pooling, pagination (from FEATURES.md table stakes)
**Avoids:** Async-over-sync pitfall, N+1 queries pitfall, connection pool exhaustion pitfall
**Research needs:** STANDARD PATTERNS — well-documented in Microsoft docs, skip /gsd:research-phase

### Phase 2: Caching Implementation
**Rationale:** After queries are optimized, caching provides 10-100x speedup. Must implement invalidation alongside caching to avoid stale data disasters in real-time app.
**Delivers:** Redis distributed cache, cache-aside pattern in services, cache invalidation service, response compression (Brotli), cache hit/miss metrics
**Uses:** Redis (StackExchangeRedis 8.0.23), response compression middleware
**Implements:** Caching layer between service and data access (from ARCHITECTURE.md)
**Avoids:** Cache stampede pitfall, cache invalidation bugs pitfall
**Research needs:** MODERATE — Redis integration patterns for ASP.NET Core well-documented, but cache invalidation strategy needs design decisions

### Phase 3: SignalR Optimization
**Rationale:** After caching reduces database load, optimize real-time communication bandwidth and prepare for horizontal scaling.
**Delivers:** Message batching, differential updates (not full state), WebSocket compression, Redis backplane configuration (not deployed yet)
**Uses:** SignalR Redis backplane (Microsoft.AspNetCore.SignalR.StackExchangeRedis 8.0.23)
**Implements:** SignalR optimization patterns (from ARCHITECTURE.md)
**Avoids:** Broadcasting full state anti-pattern from pitfalls
**Research needs:** MODERATE — SignalR backplane setup has gotchas (channel prefix, same data center requirement)

### Phase 4: Advanced Optimization
**Rationale:** After core optimizations proven, add observability to measure impact and identify remaining bottlenecks. Load testing validates capacity.
**Delivers:** OpenTelemetry instrumentation with sampling, custom business metrics, load testing pipeline (k6 or JMeter), rate limiting, covering indexes for PostgreSQL
**Uses:** OpenTelemetry (1.15.0), k6 or JMeter, BenchmarkDotNet (0.15.8), MiniProfiler (4.5.4)
**Implements:** Observability layer (from ARCHITECTURE.md)
**Avoids:** APM overhead pitfall, over-optimization pitfall
**Research needs:** LOW — OpenTelemetry integration standard, load testing tools well-documented

### Phase 5: Future Consideration (.NET 10)
**Rationale:** Emerging .NET 10 features require framework upgrade. Defer until .NET 10 adoption justified by other needs.
**Delivers:** Evaluation of HybridCache, MapStaticAssets, Native AOT compilation
**Uses:** .NET 10 features (requires upgrade from .NET 8)
**Avoids:** Premature adoption of beta features
**Research needs:** HIGH — .NET 10 still evolving, will need research when upgrade planned

### Phase Ordering Rationale

- **Foundation first** because caching broken queries doesn't help, and thread pool starvation undermines all other optimizations. Async must be correct throughout before adding distributed systems complexity.
- **Caching before SignalR optimization** because reducing database load is higher priority than optimizing real-time updates. Also, cache invalidation should be coordinated with SignalR broadcasts.
- **SignalR after caching** because message optimization has less impact than query optimization. Redis infrastructure from caching phase enables SignalR backplane.
- **Monitoring last** because it measures impact of optimizations. Need baseline (Phase 1-2) then measure improvement. APM overhead acceptable only after core optimizations validated.
- **Dependencies enforced by architecture**: Redis required for both distributed cache and SignalR backplane. Async foundation required for all I/O operations to work correctly.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 3 (SignalR Optimization):** Redis backplane has deployment gotchas (latency sensitivity, channel isolation). May need /gsd:research-phase for backplane setup patterns.
- **Phase 5 (Future Tech):** .NET 10 features require upgrade research. Definitely needs /gsd:research-phase when scheduled.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Foundation):** Async patterns, EF Core query optimization, and indexing are exhaustively documented in Microsoft Learn. Implement directly from research.
- **Phase 2 (Caching):** Redis integration for ASP.NET Core is well-established. Cache-aside pattern is standard. Research provides clear implementation guidance.
- **Phase 4 (Advanced):** OpenTelemetry instrumentation and load testing tools have extensive documentation. Follow official integration guides.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All recommendations from official Microsoft docs, NuGet packages verified, version compatibility checked for .NET 8 |
| Features | HIGH | Based on official ASP.NET Core performance best practices docs, 2026 industry guides, established optimization patterns |
| Architecture | HIGH | Integration points verified against CrowdQR codebase, patterns from official Microsoft architecture guidance, dependency ordering validated |
| Pitfalls | HIGH | Sourced from Microsoft best practices, Stack Overflow common issues, developer experience blogs, anti-pattern compilations |

**Overall confidence:** HIGH

All research grounded in official Microsoft documentation for ASP.NET Core 8.0, PostgreSQL/EF Core patterns, and SignalR scaling. Stack recommendations use production-tested packages with stable version numbers. Architecture patterns verified against CrowdQR's existing structure (Controllers → Services → DbContext → PostgreSQL).

### Gaps to Address

Areas where research was inconclusive or needs validation during implementation:

- **Cache invalidation complexity for CrowdQR's event model**: Research provides general patterns but specific invalidation graph (which cache keys to clear when a vote is recorded) needs design during Phase 2 planning. Recommendation: map entity relationships and define invalidation rules per entity type.

- **SignalR backplane performance at CrowdQR's scale**: Research indicates Redis backplane works well but actual message throughput and latency depends on deployment topology. Recommendation: load test SignalR backplane in Phase 3 with realistic client counts (100-1000 concurrent connections) before production deployment.

- **Performance budget targets**: Research suggests p95 <200ms for APIs, but CrowdQR's acceptable latency depends on user expectations and usage patterns. Recommendation: establish baselines in Phase 1, set improvement targets (e.g., 50% reduction), validate with stakeholders.

- **.NET 9 vs .NET 8 tradeoff**: Research identifies HybridCache as ideal solution (built-in stampede protection, L1/L2 cache), but it requires .NET 9+. CrowdQR is on .NET 8. Recommendation: defer HybridCache to Phase 5, use manual cache coordination (IMemoryCache + IDistributedCache) with locking for stampede protection in Phase 2.

## Sources

### Primary (HIGH confidence)

**Microsoft Official Documentation:**
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0) — async patterns, performance anti-patterns
- [Distributed caching in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-9.0) — IDistributedCache, Redis integration
- [Output caching middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-10.0) — HTTP response caching patterns
- [.NET Observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel) — telemetry instrumentation
- [Efficient Querying - EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) — AsNoTracking, split queries, projections
- [Redis backplane for SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-8.0) — multi-server scaling
- [ASP.NET Core load/stress testing](https://learn.microsoft.com/en-us/aspnet/core/test/load-tests?view=aspnetcore-10.0) — performance validation

**Official Package Sources:**
- [NuGet: Microsoft.Extensions.Caching.StackExchangeRedis 8.0.23](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/8.0.8)
- [NuGet: OpenTelemetry.Instrumentation.AspNetCore 1.15.0](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore)
- [NuGet: Microsoft.AspNetCore.SignalR.StackExchangeRedis 8.0.23](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.StackExchangeRedis)
- [BenchmarkDotNet Official Site](https://benchmarkdotnet.org/)

**Expert Resources:**
- [AspNetCoreDiagnosticScenarios - Async Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md) — David Fowler's canonical async patterns
- [Redis.io: API Caching with ASP.NET Core](https://redis.io/learn/develop/dotnet/aspnetcore/caching/basic-api-caching) — official Redis documentation
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/) — PostgreSQL optimization patterns

### Secondary (MEDIUM confidence)

**2026 Best Practices:**
- [Performance Tuning in ASP.NET Core 2026](https://www.syncfusion.com/blogs/post/performance-tuning-in-aspnetcore-2026) — current optimization techniques
- [ASP.NET Core Developer Roadmap 2026](https://logiclense.com/product/asp-net-core-developer-roadmap-2026/) — architecture patterns

**Developer Guides:**
- [Code Maze: Performance Testing with k6](https://code-maze.com/aspnetcore-performance-testing-with-k6/) — load testing setup
- [OneUpTime: Optimize Entity Framework Core Queries (2026-01-28)](https://oneuptime.com/blog/post/2026-01-28-optimize-entity-framework-core-queries/view) — recent EF optimization guide
- [Medium: Distributed Caching in ASP.NET Core 10 with Redis](https://medium.com/codetodeploy/distributed-caching-in-asp-net-core-10-with-redis-2f0c4a837c23) — implementation patterns

**Anti-Pattern Resources:**
- [Top 10 .NET Performance Anti-Patterns](https://medium.com/turbo-net/top-10-net-performance-anti-patterns-you-should-fix-today-d58f4a682340) — common mistakes
- [Don't Block on Async Code](https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html) — Stephen Cleary's async deadlock guide
- [Avoiding N+1 Queries in EF Core](https://medium.com/@kittikawin_ball/avoiding-n-1-queries-in-ef-core-practical-patterns-and-fixes-9ef8da6a6a9f) — query optimization patterns

### Tertiary (MEDIUM-LOW confidence, cross-validation applied)

**Tool Comparisons:**
- [k6 vs. JMeter for .NET Applications](https://itnext.io/k6-vs-jmeter-choosing-the-right-performance-testing-tool-for-your-net-applications-9bd3525b169a) — load testing tool selection
- [Best .NET Application Monitoring Tools in 2026](https://betterstack.com/community/comparisons/dotnet-application-monitoring-tools/) — APM provider comparison

---
*Research completed: 2026-02-05*
*Ready for roadmap: yes*
