# Roadmap: CrowdQR v1.1 Performance Enhancements

## Overview

This roadmap transforms CrowdQR from an MVP into a performant, production-ready application. Starting with async fundamentals and query optimization, we build a solid foundation before adding Redis caching and SignalR enhancements. The journey concludes with comprehensive observability, load testing validation, and production hardening to ensure the system scales efficiently under load.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation** - Async patterns, query optimization, and database indexing
- [ ] **Phase 2: Caching Layer** - Redis infrastructure and service-level caching
- [ ] **Phase 3: SignalR Optimization** - Message optimization and scalability enhancements
- [ ] **Phase 4: Observability & Validation** - Monitoring, profiling, and load testing
- [ ] **Phase 5: Production Hardening** - Rate limiting and final optimizations

## Phase Details

### Phase 1: Foundation
**Goal**: Establish async patterns throughout call stack and eliminate N+1 query bottlenecks
**Depends on**: Nothing (first phase)
**Requirements**: ASYNC-01, ASYNC-02, ASYNC-03, ASYNC-04, ASYNC-05, ASYNC-06, QUERY-01, QUERY-02, QUERY-03, QUERY-04, QUERY-05, QUERY-06, QUERY-07, INDEX-01, INDEX-02, INDEX-03, INDEX-04, INDEX-05, CONN-01, CONN-02, CONN-03
**Success Criteria** (what must be TRUE):
  1. All API endpoints respond using async/await patterns with no blocking calls
  2. Database queries for event retrieval execute without N+1 patterns (verified via query logging)
  3. Critical queries (event details, request lists) complete in under 50ms with proper indexes
  4. Connection pool metrics show no connection leaks or exhaustion under normal load
  5. Integration tests validate query counts remain under target thresholds
**Plans**: 6 plans

Plans:
- [x] 01-01-PLAN.md — EF Core infrastructure: async analyzers, DbContext pooling, connection pool config, query logging
- [x] 01-02-PLAN.md — Query optimization: EventController and DashboardController (N+1 fix, projections, AsNoTracking)
- [x] 01-03-PLAN.md — Query optimization: Request, Vote, Session, User, Reports controllers (AsNoTracking, projections)
- [x] 01-04-PLAN.md — Database indexing: foreign key indexes, composite indexes, EF Core migration
- [x] 01-05-PLAN.md — MiniProfiler: install, configure with EF Core integration, dev-only profiling UI
- [x] 01-06-PLAN.md — Integration tests: query count budgets, N+1 regression detection

### Phase 2: Caching Layer
**Goal**: Implement distributed caching with Redis to reduce database load and improve response times
**Depends on**: Phase 1
**Requirements**: CACHE-01, CACHE-02, CACHE-03, CACHE-04, RCACHE-01, RCACHE-02, RCACHE-03, RCACHE-04, SCACHE-01, SCACHE-02, SCACHE-03, SCACHE-04, SCACHE-05, VOTE-01, VOTE-02, VOTE-03, VOTE-04
**Success Criteria** (what must be TRUE):
  1. Redis container runs in Docker Compose and API connects successfully
  2. Vote counts load from cache with event-driven invalidation when votes change
  3. Cache hit rates exceed 70% for frequently accessed endpoints (events, requests)
  4. Cache invalidation happens immediately after data mutations with no stale data visible
  5. Response times for cached endpoints improve by 50%+ compared to Phase 1 baseline
**Plans**: TBD

Plans:
- [ ] TBD (to be created via /gsd:plan-phase 2)

### Phase 3: SignalR Optimization
**Goal**: Optimize real-time communication for bandwidth efficiency and horizontal scalability
**Depends on**: Phase 2
**Requirements**: SIGNALR-01, SIGNALR-02, SIGNALR-03, SIGNALR-04, SIGNALR-05, SIGNALR-06, SIGNALR-07, COMP-01, COMP-02, COMP-03, COMP-04
**Success Criteria** (what must be TRUE):
  1. SignalR messages use differential updates sending only changed fields (not full state)
  2. WebSocket compression reduces message payload sizes by 60%+ for typical updates
  3. Redis backplane configuration enables multi-server SignalR deployment capability
  4. System supports 500+ concurrent SignalR connections without performance degradation
  5. Brotli compression reduces HTTP response sizes for JSON payloads
**Plans**: TBD

Plans:
- [ ] TBD (to be created via /gsd:plan-phase 3)

### Phase 4: Observability & Validation
**Goal**: Establish comprehensive monitoring, profiling, and load testing to measure and validate improvements
**Depends on**: Phase 3
**Requirements**: PROF-01, PROF-02, PROF-03, PROF-04, OTEL-01, OTEL-02, OTEL-03, OTEL-04, OTEL-05, OTEL-06, METRIC-01, METRIC-02, METRIC-03, METRIC-04, METRIC-05, METRIC-06, METRIC-07, DASH-01, DASH-02, DASH-03, DASH-04, LOAD-01, LOAD-02, LOAD-03, LOAD-04, LOAD-05, VALID-01, VALID-02, VALID-03, VALID-04, VALID-05
**Success Criteria** (what must be TRUE):
  1. OpenTelemetry captures traces, metrics, and logs with configurable sampling
  2. Performance dashboard visualizes p50/p95/p99 response times and cache hit rates
  3. MiniProfiler shows query execution details in development environment
  4. k6 load tests validate system performance under 500+ concurrent users
  5. Before/after performance metrics document quantified improvements (response times, query counts)
**Plans**: TBD

Plans:
- [ ] TBD (to be created via /gsd:plan-phase 4)

### Phase 5: Production Hardening
**Goal**: Add rate limiting and finalize production-ready optimizations
**Depends on**: Phase 4
**Requirements**: RATE-01, RATE-02, RATE-03, RATE-04
**Success Criteria** (what must be TRUE):
  1. Rate limiting middleware protects API endpoints from abuse
  2. Rate limit responses (429) include retry-after headers and user-friendly messages
  3. Rate limiting tested under load and doesn't impact legitimate traffic
  4. All performance optimizations validated with final load test run
  5. Performance gains documented in milestone summary for team review
**Plans**: TBD

Plans:
- [ ] TBD (to be created via /gsd:plan-phase 5)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 6/6 | ✓ Complete | 2026-02-05 |
| 2. Caching Layer | 0/TBD | Not started | - |
| 3. SignalR Optimization | 0/TBD | Not started | - |
| 4. Observability & Validation | 0/TBD | Not started | - |
| 5. Production Hardening | 0/TBD | Not started | - |

---
*Created: 2026-02-05*
*Milestone: v1.1 Performance Enhancements*
