# Requirements: CrowdQR Performance Enhancements

**Defined:** 2026-02-05
**Core Value:** Audiences can influence DJ sets in real-time without disruption, and DJs can see the most popular requests instantly.

## v1.1 Requirements

Requirements for Performance Enhancements milestone. Each maps to roadmap phases.

### Async Patterns

- [ ] **ASYNC-01**: All API controllers converted to async/await
- [ ] **ASYNC-02**: All service layer methods converted to async/await
- [ ] **ASYNC-03**: All data access layer methods converted to async/await
- [ ] **ASYNC-04**: All blocking calls (.Result, .Wait()) eliminated
- [ ] **ASYNC-05**: DbContext disposal uses await using pattern
- [ ] **ASYNC-06**: IDbContextFactory used for background tasks if applicable

### Database Query Optimization

- [ ] **QUERY-01**: N+1 query pattern in EventController GetEvent fixed
- [ ] **QUERY-02**: AsNoTracking() applied to all read-only queries
- [ ] **QUERY-03**: Projections (Select) used instead of full entity materialization where beneficial
- [ ] **QUERY-04**: Eager loading (Include/ThenInclude) used for required related entities
- [ ] **QUERY-05**: AsSplitQuery() implemented for complex joins to avoid cartesian explosion
- [ ] **QUERY-06**: Query logging enabled to detect N+1 patterns
- [ ] **QUERY-07**: Integration tests verify query counts for critical paths

### Database Indexing

- [ ] **INDEX-01**: Missing indexes identified through query profiling
- [ ] **INDEX-02**: Indexes added for foreign keys used in joins
- [ ] **INDEX-03**: Covering indexes created for hot query paths
- [ ] **INDEX-04**: Composite indexes added for multi-column filter/sort queries
- [ ] **INDEX-05**: Index effectiveness validated with EXPLAIN ANALYZE

### Connection Management

- [ ] **CONN-01**: PostgreSQL connection pool sizing configured appropriately
- [ ] **CONN-02**: Connection pool metrics monitored
- [ ] **CONN-03**: Connection leaks prevented via proper disposal patterns

### Caching Infrastructure

- [ ] **CACHE-01**: Redis container added to Docker Compose
- [ ] **CACHE-02**: IDistributedCache configured with Redis for production
- [ ] **CACHE-03**: IMemoryCache configured for development/testing
- [ ] **CACHE-04**: Cache configuration supports multiple environments

### Response Caching

- [ ] **RCACHE-01**: Output caching middleware added to API
- [ ] **RCACHE-02**: Cache policies configured for GET endpoints
- [ ] **RCACHE-03**: Cache headers set appropriately for responses
- [ ] **RCACHE-04**: Cache invalidation on data mutations

### Service-Level Caching

- [ ] **SCACHE-01**: Cache-aside pattern implemented in services
- [ ] **SCACHE-02**: Cache stampede protection via SemaphoreSlim
- [ ] **SCACHE-03**: Cache keys designed for efficient invalidation
- [ ] **SCACHE-04**: Cache TTL configured per data type
- [ ] **SCACHE-05**: Cache invalidation strategy documented and implemented

### Vote Count Optimization

- [ ] **VOTE-01**: Vote count calculation made asynchronous
- [ ] **VOTE-02**: Vote counts cached with event-driven invalidation
- [ ] **VOTE-03**: Cache invalidated on SignalR vote broadcasts
- [ ] **VOTE-04**: Vote count performance improved measurably

### SignalR Message Optimization

- [ ] **SIGNALR-01**: Message batching implemented for multiple updates
- [ ] **SIGNALR-02**: Differential updates send only changed fields
- [ ] **SIGNALR-03**: WebSocket compression enabled
- [ ] **SIGNALR-04**: Connection handling optimized for memory efficiency

### SignalR Scalability

- [ ] **SIGNALR-05**: Redis backplane configured for SignalR
- [ ] **SIGNALR-06**: Backplane validated under load testing
- [ ] **SIGNALR-07**: Connection pooling configured

### Development Profiling

- [ ] **PROF-01**: MiniProfiler installed and configured for development
- [ ] **PROF-02**: Database query profiling enabled
- [ ] **PROF-03**: Profiling baselines established for critical paths
- [ ] **PROF-04**: Profiling available in development environment

### OpenTelemetry Monitoring

- [ ] **OTEL-01**: OpenTelemetry packages installed
- [ ] **OTEL-02**: Auto-instrumentation configured for ASP.NET Core
- [ ] **OTEL-03**: Auto-instrumentation configured for EF Core
- [ ] **OTEL-04**: Auto-instrumentation configured for HttpClient
- [ ] **OTEL-05**: Sampling configured (10% default, 100% errors)
- [ ] **OTEL-06**: Trace export configured to APM provider

### Performance Metrics

- [ ] **METRIC-01**: API endpoint response times tracked (p50, p95, p99)
- [ ] **METRIC-02**: Database query counts tracked per request
- [ ] **METRIC-03**: Database query duration tracked
- [ ] **METRIC-04**: Cache hit/miss rates tracked
- [ ] **METRIC-05**: SignalR connection counts tracked
- [ ] **METRIC-06**: Connection pool metrics tracked
- [ ] **METRIC-07**: Custom business metrics added where valuable

### Performance Dashboard

- [ ] **DASH-01**: Performance metrics visualized in monitoring UI
- [ ] **DASH-02**: Critical performance indicators surfaced
- [ ] **DASH-03**: Alerts configured for performance degradation
- [ ] **DASH-04**: Dashboard accessible to team

### Load Testing Infrastructure

- [ ] **LOAD-01**: k6 installed and configured
- [ ] **LOAD-02**: Load test scripts created for critical user paths
- [ ] **LOAD-03**: WebSocket testing configured for SignalR
- [ ] **LOAD-04**: Load test environment configured
- [ ] **LOAD-05**: CI/CD integration for load testing

### Performance Validation

- [ ] **VALID-01**: Baseline performance metrics captured before optimizations
- [ ] **VALID-02**: Before/after performance comparisons documented
- [ ] **VALID-03**: System validated under 500+ concurrent users
- [ ] **VALID-04**: Performance improvements quantified (response times, query counts)
- [ ] **VALID-05**: Performance gains documented in milestone summary

### Compression

- [ ] **COMP-01**: Brotli compression enabled for HTTP responses
- [ ] **COMP-02**: WebSocket compression enabled for SignalR
- [ ] **COMP-03**: Compression levels configured appropriately
- [ ] **COMP-04**: Compression validated under load

### Rate Limiting

- [ ] **RATE-01**: Rate limiting middleware added
- [ ] **RATE-02**: Rate limits configured per endpoint type
- [ ] **RATE-03**: Rate limit responses (429) handled gracefully
- [ ] **RATE-04**: Rate limiting tested under load

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Future .NET Optimizations

- **NET10-01**: Evaluate HybridCache for L1/L2 caching with stampede protection (.NET 9+)
- **NET10-02**: Evaluate MapStaticAssets for frontend optimization (.NET 9+)
- **NET10-03**: Consider Native AOT compilation for startup performance (.NET 10+)

### Advanced Scalability

- **SCALE-01**: Multi-region deployment with geo-distributed Redis
- **SCALE-02**: Kubernetes orchestration for horizontal scaling
- **SCALE-03**: CDN integration for static assets
- **SCALE-04**: Read replicas for PostgreSQL

## Out of Scope

| Feature | Reason |
|---------|--------|
| Spotify/YouTube integration | Feature addition, not performance work—separate milestone |
| OAuth login | New authentication method, deferred to future milestone |
| Mobile PWA | New platform, not performance optimization |
| DJ analytics dashboard | New feature, separate milestone |
| Horizontal auto-scaling | Infrastructure complexity beyond v1.1 scope |
| Multi-region deployment | Geographic distribution not needed for single-venue use case |
| GraphQL API | API paradigm change, not performance optimization |
| Microservices architecture | Over-engineering for current scale |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| (To be filled by roadmapper) | — | Pending |

**Coverage:**
- v1.1 requirements: 76 total
- Mapped to phases: (pending roadmap creation)
- Unmapped: (pending roadmap creation)

---
*Requirements defined: 2026-02-05*
*Last updated: 2026-02-05 after initial definition*
