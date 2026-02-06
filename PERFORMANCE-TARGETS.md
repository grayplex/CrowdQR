# Performance Targets

**Phase:** 01-foundation
**Target:** <50ms p95 for read endpoints (CONTEXT.md locked decision)

## Query Count Budgets

| Endpoint Category | Budget | Example Endpoints |
|-------------------|--------|-------------------|
| Simple GET by ID | 1 query | GET /api/event/{id}, GET /api/request/{id} |
| GET with related data | 1-2 queries | GET /api/event/{id} (with requests + vote counts) |
| List endpoints | 1 query | GET /api/event, GET /api/request/event/{eventId} |
| Dashboard summary | 2-3 queries | GET /api/dashboard/event/{eventId}/summary |
| Report generation | 2-3 queries | GET /api/dashboard/dj/{djUserId}/event-stats |
| Write operations | 2-4 queries | POST (validate + write + notify) |

## Response Time Targets

| Metric | Target | Notes |
|--------|--------|-------|
| p50 | <20ms | Typical read endpoint |
| p95 | <50ms | Aggressive target per CONTEXT.md |
| p99 | <100ms | Acceptable outlier |

## Success Metrics

- Response time improvements (p50/p95/p99) vs baseline
- Query count reductions vs baseline
- Database load reduction (CPU/IO)

---
*Documented targets without automated alerts (CONTEXT.md decision)*
*Budgets enforced via integration tests in Plan 06*
