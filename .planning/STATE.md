# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-05)

**Core value:** Audiences can influence DJ sets in real-time without disruption, and DJs can see the most popular requests instantly.
**Current focus:** Phase 1 - Foundation

## Current Position

Phase: 1 of 5 (Foundation)
Plan: 6 of 6 in current phase
Status: Phase complete
Last activity: 2026-02-05 — Completed 01-06 (Query count integration tests)

Progress: [██████░░░░] 100% Phase 1 complete

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 7 min
- Total execution time: 0.7 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation | 6/6 | 43 min | 7 min |

**Recent Trend:**
- Last 3 plans: 01-04 (5min), 01-05 (4min), 01-06 (10min)
- Trend: Consistent velocity, final plan slightly longer (test infrastructure setup)

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

| ID | Phase | Decision | Impact |
|----|-------|----------|--------|
| ASYNC-01 | 01-01 | Suppress VSTHRD200 naming warnings (follow-up task) | Unblocks build, 74 methods need Async suffix |
| POOL-01 | 01-01 | DbContext pooling safe (no private state) | 2x faster context creation |
| CONN-01 | 01-01 | Connection pool sizing: max 50, min 5 | Sized for production load |
| PROJ-01 | 01-02 | Use server-side projections for all read endpoints | 90%+ reduction in data loaded into memory |
| TRACK-01 | 01-02 | Apply AsNoTracking to all GET endpoints | Eliminates change tracker overhead |
| SPLIT-01 | 01-02 | Use separate queries to avoid cartesian explosion | Linear query complexity instead of multiplicative |
| PROJ-02 | 01-03 | Vote counts via SQL COUNT in projections | Avoids loading full vote collections into memory |
| AUTH-01 | 01-03 | Extract auth fields via Select before full query | Minimizes data transfer for authorization checks |
| INDEX-01 | 01-04 | Index all foreign key columns - PostgreSQL does NOT auto-index FKs | Every JOIN needs indexed FK to avoid sequential scans |
| INDEX-02 | 01-04 | Composite (EventId, Status) index on Request table | Covers dashboard hot path and standalone EventId queries via leftmost prefix |
| INDEX-03 | 01-04 | Keep standalone EventId index despite composite overlap | Defer removal to Phase 4 query analysis rather than premature optimization |
| INDEX-04 | 01-04 | Explicit index naming convention (IX_Table_Column) | Migration predictability and clarity over auto-generated names |
| PROF-01 | 01-05 | MiniProfiler enabled only in development | Zero production overhead, profiling only during development |
| PROF-02 | 01-05 | Query count budgets per endpoint category | 1 query for simple GETs, 1-2 for related data, 2-3 for dashboards |
| PROF-03 | 01-05 | Response time targets: p50 <20ms, p95 <50ms, p99 <100ms | CONTEXT.md locked decision for aggressive performance |
| TEST-01 | 01-06 | Use DbCommandInterceptor for query counting | Accurate count of all database commands with thread-safe implementation |
| TEST-02 | 01-06 | Use TestAuthenticationHandler for integration tests | Simpler test setup without password/token management |
| TEST-03 | 01-06 | Generous query budgets to avoid flaky tests | Allow 1-2 extra queries while still catching N+1 patterns (10+ queries) |

**Milestone decisions:**
- v1.1 Milestone: Follow foundation-first approach (async + queries before caching)
- v1.1 Milestone: Use Redis for distributed cache and SignalR backplane
- v1.1 Milestone: OpenTelemetry for vendor-agnostic observability

### Pending Todos

- 01-01: Fix 74 VSTHRD200 violations (add Async suffix to method names) - currently suppressed

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-05
Stopped at: Completed Phase 1 (01-06: Query count integration tests)
Resume file: None
Next: Phase 2 - Caching & SignalR

---
*This is the first GSD milestone for CrowdQR*
*Phase numbering starts from 1*
