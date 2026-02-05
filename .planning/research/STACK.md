# Stack Research: Performance Optimization

**Domain:** Performance optimization, caching, monitoring, and load testing for ASP.NET Core 8.0
**Researched:** 2026-02-05
**Confidence:** HIGH

## Recommended Stack

### Caching Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Microsoft.Extensions.Caching.Memory | 8.0.x (built-in) | In-memory caching for single-server scenarios | Built into ASP.NET Core, zero infrastructure, fastest access times. Use for development, testing, or single-server deployments where cache doesn't need to survive restarts. |
| Microsoft.Extensions.Caching.StackExchangeRedis | 8.0.23 | Distributed caching via Redis for IDistributedCache | Industry standard for distributed caching. Survives restarts, works across server farms, reduces database load. Essential for production multi-server deployments. |
| Microsoft.AspNetCore.OutputCaching.StackExchangeRedis | 8.0.23 | Redis-backed output caching for HTTP responses | New in ASP.NET Core 7+, superior to response caching. Caches entire HTTP responses with server-side control, tag-based invalidation, and Redis backing for distributed scenarios. |

### Monitoring and Observability

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.0 | Distributed tracing and metrics collection | Industry standard for observability. Native .NET 8 integration, vendor-agnostic (works with Datadog, Elastic, Grafana, etc.). Provides automatic instrumentation for HTTP requests, database calls, and custom traces. |
| OpenTelemetry.Instrumentation.Http | 1.15.0 | HTTP client instrumentation | Traces outgoing HTTP calls automatically, critical for understanding external dependencies and API call chains. |
| OpenTelemetry.Instrumentation.EntityFrameworkCore | 6.0.0-beta.11 | EF Core query tracing | Visibility into database query performance, N+1 detection, query duration tracking. |
| MiniProfiler.AspNetCore | 4.5.4 | Development profiling UI | Lightweight in-app profiler with visual UI. Perfect for development and staging to identify slow queries, N+1 problems, and blocking operations in real-time. |

### Load Testing Tools

| Tool | Purpose | Why Recommended |
|------|---------|-----------------|
| k6 | Modern load testing with code-based scripts | Written in Go, scripts in JavaScript. Developer-friendly, excellent CI/CD integration, lightweight. Best for modern DevOps workflows. Grafana ecosystem integration for dashboards. |
| Apache JMeter | GUI-based load testing | Mature, comprehensive testing tool with GUI. Better for complex test scenarios requiring visual configuration. More resource-intensive than k6 but provides extensive protocol support. |

### Benchmarking

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| BenchmarkDotNet | 0.15.8 | Micro-benchmarking for code performance | Official .NET benchmarking tool used by Microsoft. Statistical rigor, protects against common benchmarking mistakes, generates reproducible results. Essential for measuring optimization impact. |

### SignalR Scale-Out

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Microsoft.AspNetCore.SignalR.StackExchangeRedis | 8.0.23 | Redis backplane for SignalR multi-server | Required for SignalR in multi-server deployments. Routes messages between servers via Redis pub/sub. Must run in same data center as app servers to avoid latency penalties. |

### EF Core Optimization (No New Packages)

Built-in EF Core features for performance:
- `AsNoTracking()` - Disable change tracking for read-only queries
- `AsSplitQuery()` - Prevent cartesian explosion in multi-include queries
- `Include()` / `ThenInclude()` - Eager loading to prevent N+1
- Compiled Queries - Pre-compile LINQ for high-frequency queries

## Installation

```bash
# Distributed Caching
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.23
dotnet add package Microsoft.AspNetCore.OutputCaching.StackExchangeRedis --version 8.0.23

# OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting --version 1.15.0
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.15.0
dotnet add package OpenTelemetry.Instrumentation.Http --version 1.15.0
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 6.0.0-beta.11
dotnet add package OpenTelemetry.Exporter.Console --version 1.15.0

# SignalR Scale-Out (only if multi-server)
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis --version 8.0.23

# Development Tools
dotnet add package MiniProfiler.AspNetCore --version 4.5.4
dotnet add package BenchmarkDotNet --version 0.15.8

# Load Testing (install globally, not in project)
# k6: Download from https://k6.io/docs/getting-started/installation/
# JMeter: Download from https://jmeter.apache.org/download_jmeter.cgi
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| Redis (StackExchangeRedis) | SQL Server distributed cache | Use SQL cache if Redis infrastructure unavailable, but expect slower performance and higher database load. |
| Redis (StackExchangeRedis) | In-memory cache only | Use in-memory for single-server deployments or when cache persistence not required. Development/testing scenarios. |
| OpenTelemetry | Application Insights SDK directly | Use Application Insights SDK if locked into Azure ecosystem and not planning multi-cloud. OpenTelemetry is more portable. |
| OpenTelemetry | Datadog native SDK | Use Datadog SDK if deeply integrated with Datadog features. OpenTelemetry provides vendor independence. |
| k6 | JMeter | Use JMeter if team prefers GUI-based configuration or needs extensive protocol support beyond HTTP/WebSocket. |
| Output Caching | Response Caching | Never. Output caching is superior: server-side control, tag-based invalidation, distributed support. Response caching is HTTP header-based and client-controlled. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Response Caching (ResponseCacheAttribute) | Replaced by Output Caching in ASP.NET Core 7+. Client-controlled via HTTP headers, cannot be managed server-side. | Output Caching middleware with Redis backing |
| HybridCache | .NET 9+ only, not available for .NET 8. Don't try backporting. | IDistributedCache + IMemoryCache pattern manually, or wait for .NET 9 upgrade |
| ConfigureAwait(false) in ASP.NET Core | ASP.NET Core removed synchronization context, making ConfigureAwait(false) redundant and no-op. | Nothing. Just use await normally in ASP.NET Core code. |
| Task.Wait() or Task.Result | Causes thread pool starvation and deadlocks. Major performance killer. | Always use async/await throughout the call chain |
| Lazy loading in EF Core (without explicit strategy) | Causes N+1 query problems silently. Performance disaster. | Explicit eager loading with Include() or use projections with Select() |

## Stack Patterns by Variant

**If single-server deployment (development, small production):**
- Use `IMemoryCache` for distributed cache abstraction
- Skip SignalR Redis backplane
- Skip Redis-backed output caching (use memory-backed)
- Keep MiniProfiler enabled for ongoing monitoring

**If multi-server deployment (production, scaling):**
- Use Redis for `IDistributedCache`
- Use Redis for output caching
- Configure SignalR Redis backplane with channel prefix
- Export OpenTelemetry to APM provider (Datadog, Elastic, Grafana)
- Disable MiniProfiler in production (use OpenTelemetry instead)

**If high-frequency read operations:**
- Implement output caching aggressively on GET endpoints
- Use Redis with appropriate expiration policies
- Add AsNoTracking() to all read-only EF queries
- Consider compiled queries for hot paths

**If complex object graphs with EF Core:**
- Use AsSplitQuery() to prevent cartesian explosion
- Implement projection with Select() for DTOs
- Avoid Include() for more than 2-3 levels deep
- Consider separating queries instead of mega-includes

## Integration with Existing Stack

### PostgreSQL with EF Core
- Add AsNoTracking() to existing queries in read-only endpoints
- Identify N+1 patterns with MiniProfiler, fix with Include()
- Use AsSplitQuery() for complex queries with multiple includes
- Add compiled queries for hot paths (search, listings)

### SignalR Real-Time
- Add Redis backplane if deploying to multiple servers
- Configure connection pooling in Redis configuration
- Set appropriate channel prefix to isolate SignalR traffic
- Monitor backplane latency (must be <5ms for good UX)

### Docker Compose
- Add Redis service to docker-compose.yml
- Configure health checks for Redis
- Ensure Redis runs in same network as app services
- For production, use Redis persistence (RDB or AOF)

### Session-Based Authentication
- Cache session data in Redis for distributed scenarios
- Configure session timeout to match cache expiration
- Use sticky sessions (session affinity) if possible to reduce Redis load

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| Microsoft.Extensions.Caching.StackExchangeRedis 8.0.23 | ASP.NET Core 8.0.x | Version aligned with .NET 8. For .NET 9+, use 9.x versions. |
| OpenTelemetry.Instrumentation.AspNetCore 1.15.0 | .NET 8.0, .NET Standard 2.0 | Cross-version compatible. Works with .NET 6-10. |
| OpenTelemetry.Instrumentation.EntityFrameworkCore 6.0.0-beta.11 | EF Core 6.0+ | Beta status but stable. No breaking changes expected. |
| Microsoft.AspNetCore.SignalR.StackExchangeRedis 8.0.23 | ASP.NET Core 8.0.x, SignalR 8.0 | Must match ASP.NET Core major version. |
| Microsoft.AspNetCore.OutputCaching.StackExchangeRedis 8.0.23 | ASP.NET Core 8.0.x | Output caching available ASP.NET Core 7+. |
| MiniProfiler.AspNetCore 4.5.4 | .NET 8.0+ | Works across .NET versions. |
| BenchmarkDotNet 0.15.8 | .NET 8.0+ | Universal compatibility. |

## Configuration Best Practices

### Redis Connection String Format
```
localhost:6379,abortConnect=false,ssl=false,password=yourpassword
```
- `abortConnect=false`: Don't crash on Redis unavailability
- Set reasonable timeouts: `connectTimeout=5000,syncTimeout=5000`
- Use connection multiplexing (default in StackExchange.Redis)

### OpenTelemetry Exporter Selection
- **Development**: Console exporter for visibility
- **Staging/Production**: OTLP exporter to APM provider
- Configure sampling for high-volume apps (e.g., 10% sampling)
- Add resource attributes: service name, version, environment

### Output Caching Strategy
- Cache GET endpoints only
- Set appropriate expiration (balance freshness vs performance)
- Use tag-based invalidation for related data changes
- VaryByHeader for user-specific responses (Authorization)
- VaryByQueryKeys for parameterized endpoints

### EF Core Query Optimization
- Apply AsNoTracking() globally for read-only contexts
- Use AsSplitQuery() when Include() causes large result sets
- Monitor query execution time with MiniProfiler
- Set query splitting strategy per query, not globally

## Sources

- [Microsoft Learn: Distributed caching in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-9.0) - HIGH confidence, official documentation
- [NuGet: Microsoft.Extensions.Caching.StackExchangeRedis 8.0.23](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/8.0.8) - HIGH confidence, official package
- [Redis.io: API Caching with ASP.NET Core and Redis](https://redis.io/learn/develop/dotnet/aspnetcore/caching/basic-api-caching) - HIGH confidence, official Redis documentation
- [Microsoft Learn: Output caching middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-10.0) - HIGH confidence, official documentation
- [Microsoft Learn: .NET Observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel) - HIGH confidence, official documentation
- [OpenTelemetry: ASP.NET Core Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md) - HIGH confidence, official OpenTelemetry docs
- [NuGet: OpenTelemetry.Instrumentation.AspNetCore 1.15.0](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore) - HIGH confidence, official package
- [Microsoft Learn: Redis backplane for ASP.NET Core SignalR scale-out](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-8.0) - HIGH confidence, official documentation
- [Microsoft Learn: Efficient Querying - EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) - HIGH confidence, official documentation
- [Code Maze: Performance Testing of ASP.NET Core APIs With k6](https://code-maze.com/aspnetcore-performance-testing-with-k6/) - MEDIUM confidence, reputable .NET community source
- [Medium: k6 vs. JMeter for .NET Applications](https://itnext.io/k6-vs-jmeter-choosing-the-right-performance-testing-tool-for-your-net-applications-9bd3525b169a) - MEDIUM confidence, developer comparison
- [BenchmarkDotNet Official Site](https://benchmarkdotnet.org/) - HIGH confidence, official project site
- [NuGet: BenchmarkDotNet 0.15.8](https://www.nuget.org/packages/BenchmarkDotNet) - HIGH confidence, official package
- [Better Stack: Best .NET Application Monitoring Tools in 2026](https://betterstack.com/community/comparisons/dotnet-application-monitoring-tools/) - MEDIUM confidence, current comparison
- [NuGet: MiniProfiler.AspNetCore 4.5.4](https://www.nuget.org/packages/MiniProfiler.AspNetCore) - HIGH confidence, official package
- [Microsoft Learn: ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0) - HIGH confidence, official documentation
- [Medium: Asynchronous programming in .NET 8 — Common Pitfalls and Recommended Practices](https://admirmujkic.medium.com/asynchronous-programming-in-net-8-common-pitfalls-and-recommended-practices-593c9984d229) - MEDIUM confidence, developer practices
- [OneUpTime: How to Optimize Entity Framework Core Queries (2026-01-28)](https://oneuptime.com/blog/post/2026-01-28-optimize-entity-framework-core-queries/view) - MEDIUM confidence, recent optimization guide
- [Microsoft Learn: HybridCache library in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0) - HIGH confidence, official documentation (noted as .NET 9+ only)

---
*Stack research for: CrowdQR Performance Optimization*
*Researched: 2026-02-05*
