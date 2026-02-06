# Phase 1: Foundation - Context

**Gathered:** 2026-02-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish async patterns throughout the call stack and eliminate N+1 query bottlenecks through query optimization and database indexing. This phase focuses on performance fundamentals - asynchronous operations, efficient database queries, proper indexing, and connection pool management.

</domain>

<decisions>
## Implementation Decisions

### Async Migration Strategy
- **Bottom-up approach**: Convert data layer (repositories, database code) to async first, then work up through services to controllers
- **Mixed mode handling**: Claude's discretion - handle sync/async boundaries safely during transition
- **Enforcement**: Add static analyzers that fail builds on violations (sync-over-async, missing ConfigureAwait, etc.)
- **ConfigureAwait**: Use project-level setting (.csproj ConfigureAwaitOptions) rather than explicit calls everywhere

### Query Optimization Approach
- **Detection tools**: Use all available tools - EF Core query logging, MiniProfiler (dev only), Application Insights (production)
- **Query count budgets**: Claude's discretion - set appropriate query budgets based on endpoint complexity
- **Loading strategy**: Eager loading only (.Include) - disable lazy loading to prevent accidental N+1 patterns
- **Threshold violations**: Claude's discretion - implement appropriate alerting for slow queries in development

### Development Visibility
- **Visible metrics**: Query execution times, query counts per request, connection pool statistics
- **Visibility method**: Mix of console logs (backend) + MiniProfiler UI (request-level details)
- **Configuration**: Use appsettings.Development.json to control profiling - standard .NET approach
- **Warning severity**: Claude's discretion - set appropriate warning levels based on issue severity

### Performance Baselines
- **Response time targets**: Aggressive - <50ms p95 for read endpoints
- **Critical endpoints**: All endpoints treated equally important (event details, song request list, vote submission all matter)
- **Formalization**: Documented targets without automated alerts - set expectations without tooling burden
- **Success metrics**: Measure response time improvements (p50/p95/p99), query count reductions, and database load reduction (CPU/IO)

### Claude's Discretion
- Sync/async boundary handling during migration period
- Specific query count budgets per endpoint based on complexity
- Threshold violation alerting implementation in development
- Warning severity levels for performance issues
- Implementation details for profiling and monitoring

</decisions>

<specifics>
## Specific Ideas

- **Aggressive performance target**: <50ms p95 response times show commitment to excellent UX
- **Comprehensive tooling approach**: Using query logging, MiniProfiler, and APM together ensures visibility at all levels
- **Strict eager loading**: Disabling lazy loading entirely prevents sneaky N+1 queries from creeping in
- **Multi-metric success tracking**: Looking at response times, query counts, AND database load gives complete picture

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-02-05*
