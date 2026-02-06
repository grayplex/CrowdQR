# Phase 01 Baseline Metrics

**Captured:** 2026-02-05
**Environment:** Code Analysis (pre-optimization)
**Method:** Static analysis of controller code and EF Core query patterns

## Query Patterns Analysis (Pre-Optimization)

### EventController

| Endpoint | Current Pattern | Query Issues | Impact |
|----------|----------------|--------------|---------|
| `GET /api/event` | `.Include(e => e.DJ)` then in-memory `.Select()` | Fetches full entity graphs, then projects in-memory | Loads unnecessary columns (passwords, timestamps, etc.) |
| `GET /api/event/{id}` | `.Include(e => e.DJ).Include(e => e.Requests).ThenInclude(r => r.Votes)` | **N+1 pattern: Loads ALL votes for ALL requests into memory**, then counts in-memory | **CRITICAL: For event with 100 requests × 50 votes = 5000 Vote entities loaded** |
| `GET /api/event/slug/{slug}` | `.Include(e => e.DJ).Include(e => e.Requests).ThenInclude(r => r.Votes)` | **Same N+1 as GetEvent** | **CRITICAL: Identical issue** |
| `GET /api/event/dj/{djUserId}` | `.Where().Include(e => e.DJ)` then in-memory `.Select()` | Loads full DJ entity unnecessarily | Minor overhead |

**Tracking Status:** All GET endpoints track entities in change tracker despite being read-only operations.

### DashboardController

| Endpoint | Current Pattern | Query Issues | Impact |
|----------|----------------|--------------|---------|
| `GET /api/dashboard/event/{eventId}/summary` | Two separate queries: (1) `.Include(r => r.User).Include(r => r.Votes)` for requests, (2) `.Include(s => s.User)` for sessions | **Loads ALL votes for ALL requests into memory**, then filters/counts in-memory | **CRITICAL: Same N+1 as EventController + double entity loading for Users** |
| `GET /api/dashboard/event/{eventId}/top-requests` | `.Include(r => r.User).Include(r => r.Votes)` then **in-memory OrderByDescending(r => r.Votes.Count)** | **Loads ALL votes**, sorts in-memory instead of SQL ORDER BY | **CRITICAL: Cannot use database indexes for sorting** |
| `GET /api/dashboard/dj/{djUserId}/event-stats` | Two separate queries: (1) Get events, (2) Get all requests with `.Include(r => r.Votes)`, then in-memory joins and counts | **Loads ALL votes**, performs joins/aggregations in-memory | **CRITICAL: N+1 pattern + client-side aggregation** |

**Tracking Status:** All GET endpoints track entities unnecessarily.

## Estimated Query Counts (Example: Event with 100 requests, 50 votes each)

### Before Optimization

**EventController.GetEvent:**
- 1 query: SELECT Event + JOIN DJ
- 1 query: SELECT Requests WHERE EventId = X
- 1 query: SELECT Votes WHERE RequestId IN (100 request IDs) ← **Loads 5000 Vote rows**
- **Total:** 3 queries, **5000+ rows loaded into memory**

**DashboardController.GetEventSummary:**
- 1 query: SELECT * FROM Events WHERE EventId = X (event check)
- 1 query: SELECT Requests + JOIN User WHERE EventId = X
- 1 query: SELECT Votes WHERE RequestId IN (...) ← **Loads 5000 Vote rows**
- 1 query: SELECT Sessions + JOIN User WHERE EventId = X
- **Total:** 4 queries, **5000+ Vote rows loaded into memory**

**DashboardController.GetTopRequests:**
- 1 query: SELECT * FROM Events WHERE EventId = X (event check)
- 1 query: SELECT Requests + JOIN User WHERE EventId = X AND Status = 'Pending'
- 1 query: SELECT Votes WHERE RequestId IN (...) ← **Loads ALL votes for pending requests**
- **In-memory sort by Vote count** (cannot use database indexes)
- **Total:** 3 queries, **all votes loaded, sort in-memory**

**DashboardController.GetDJEventStats:**
- 1 query: SELECT * FROM Users WHERE UserId = X AND Role = 'DJ'
- 1 query: SELECT * FROM Events WHERE DjUserId = X
- 1 query: SELECT Requests + JOIN Votes WHERE EventId IN (...) ← **Loads ALL requests + ALL votes**
- **In-memory grouping, counting, summing**
- **Total:** 3 queries, **massive data transfer for aggregation that should happen in SQL**

## Memory Allocation Issues

1. **Change Tracking Overhead:** EF Core tracks all loaded entities for change detection, even on read-only queries
2. **Entity Materialization:** Full entity objects created with all properties, even when only a few columns needed
3. **Collection Loading:** Entire Vote collections loaded into memory when only counts are needed
4. **In-Memory Operations:** Sorting, grouping, and aggregation happen in C# instead of SQL

## Database Load Estimation

- **Connection pool usage:** Moderate (queries are async, but inefficient)
- **Network transfer:** High (thousands of unnecessary Vote rows transferred)
- **Database CPU:** Low (simple queries, but no aggregation pushdown)
- **Application memory:** High (large entity graphs in memory)

## Target Improvements for Plans 02-03

1. **AsNoTracking:** Eliminate change tracker overhead on all GET endpoints
2. **Server-side projections:** Use `.Select()` in EF Core query (before `.ToListAsync()`) to generate SQL projections
3. **SQL COUNT subqueries:** Replace `r.Votes.Count` in projections to avoid loading Vote entities
4. **SQL aggregation:** Push grouping/summing to database
5. **SQL sorting:** Use ORDER BY in database instead of in-memory sorting

## Expected Post-Optimization Results

**EventController.GetEvent (optimized):**
- 1 query: `SELECT Event.*, DJ.UserId, DJ.Username, (SELECT COUNT(*) FROM Votes WHERE ...) AS VoteCount ...`
- **Total:** 1 query, **~100 rows** (event + requests, no votes loaded)

**DashboardController.GetEventSummary (optimized):**
- 1 query: Event check (`.AnyAsync()`)
- 1 query: Requests with User.Username and SQL COUNT subqueries
- 1 query: Sessions with User info
- **Total:** 3 queries, **~100 request rows + ~50 session rows, 0 votes loaded**

**Estimated improvement:** 90%+ reduction in rows loaded into memory, 50%+ reduction in query execution time.
