# CrowdQR

## What This Is

CrowdQR is a real-time DJ audience interaction web platform that allows live audiences to submit and vote on song requests by scanning a QR code. DJs manage requests through a secure admin dashboard with live updates via SignalR.

## Core Value

Audiences can influence DJ sets in real-time without disruption, and DJs can see the most popular requests instantly.

## Current Milestone: v1.1 Performance Enhancements

**Goal:** Eliminate performance bottlenecks and add monitoring to ensure CrowdQR scales efficiently under load.

**Target improvements:**
- Fix N+1 query patterns in EventController
- Make vote count calculations asynchronous
- Implement query result caching (Redis/in-memory)
- Audit and optimize database queries, API endpoints, SignalR broadcasts, and frontend loading
- Add application performance monitoring (APM) and metrics collection
- Establish benchmarking and load testing infrastructure
- Measure and validate response time improvements

## Requirements

### Validated

<!-- Shipped and confirmed valuable from MVP. -->

**Authentication & Authorization**
- ✓ User registration with email verification — v1.0
- ✓ DJ role registration — v1.0
- ✓ Session-based authentication — v1.0
- ✓ Password reset via email link — v1.0

**Event Management**
- ✓ DJs can create events with name and slug — v1.0
- ✓ DJs can activate/deactivate events — v1.0
- ✓ QR code generation for event URLs — v1.0
- ✓ Event-based session tracking — v1.0

**Song Requests**
- ✓ Audience members can submit song requests (song name + artist) — v1.0
- ✓ Real-time request display via SignalR — v1.0
- ✓ Request status tracking (pending/approved/rejected) — v1.0

**Voting System**
- ✓ Users can vote on pending requests — v1.0
- ✓ Vote toggling (add/remove vote) — v1.0
- ✓ Vote count display and sorting — v1.0
- ✓ Vote limiting per user/session — v1.0

**DJ Dashboard**
- ✓ Request queue with vote counts — v1.0
- ✓ Approve/reject requests in real-time — v1.0
- ✓ Tab navigation (pending/approved/rejected) — v1.0
- ✓ Request search functionality — v1.0
- ✓ Active user count display — v1.0

**Infrastructure**
- ✓ PostgreSQL database with EF Core — v1.0
- ✓ Docker containerization (API + Web + DB) — v1.0
- ✓ Docker Compose deployment — v1.0
- ✓ Health check endpoints — v1.0
- ✓ Rate limiting and spam protection — v1.0

### Active

<!-- Current scope for v1.1 Performance Enhancements milestone. -->

**Performance Fixes**
- [ ] Fix N+1 query pattern in EventController GetEvent
- [ ] Make vote count calculation asynchronous
- [ ] Implement query result caching

**Database Optimization**
- [ ] Audit and add missing database indexes
- [ ] Optimize slow queries identified in audit
- [ ] Reduce query counts through eager loading

**API Optimization**
- [ ] Benchmark API endpoint response times (baseline)
- [ ] Optimize serialization overhead
- [ ] Profile and optimize slow endpoints

**SignalR Optimization**
- [ ] Optimize broadcast performance for high user counts
- [ ] Profile connection handling and memory usage
- [ ] Implement connection pooling if needed

**Frontend Optimization**
- [ ] Analyze and reduce JavaScript bundle size
- [ ] Optimize asset loading and caching
- [ ] Improve initial page load times

**Monitoring & Observability**
- [ ] Add application performance monitoring (APM)
- [ ] Implement metrics collection (response times, query counts)
- [ ] Add performance logging and tracing
- [ ] Create performance dashboard

**Validation**
- [ ] Establish load testing infrastructure
- [ ] Run before/after performance benchmarks
- [ ] Validate improvements under concurrent load
- [ ] Document performance gains

### Out of Scope

- **Spotify/YouTube integration** — Feature addition, not performance work
- **OAuth login** — New authentication method, deferred to future milestone
- **Mobile PWA** — New platform, not performance optimization
- **DJ analytics dashboard** — New feature, separate milestone
- **Horizontal scaling/clustering** — Infrastructure change beyond v1.1 scope
- **CDN setup** — Infrastructure enhancement, defer unless critical

## Context

**Technical Environment:**
- **Stack:** ASP.NET Core 8.0, Razor Pages, PostgreSQL, SignalR, Docker
- **Current Scale:** Designed for single venue/small audience (dozens to hundreds of concurrent users)
- **Known Issues:**
  - N+1 queries in event retrieval
  - Synchronous vote counting blocks request processing
  - No caching layer results in repeated database hits
  - No performance monitoring or metrics collection

**Performance Goals:**
- Critical API endpoints < 100ms response time
- Support 500+ concurrent SignalR connections
- Database query count reduced by 50%+
- Measurable improvements in perceived page load speed

**Codebase State:**
- Codebase mapped in `.planning/codebase/`
- Testing infrastructure exists (unit + integration tests)
- CI/CD pipeline in place via GitHub Actions
- Docker Compose deployment ready

## Constraints

- **Tech Stack**: Continue with ASP.NET Core 8.0, PostgreSQL — no major technology changes
- **Deployment**: Must remain Docker Compose compatible — no Kubernetes/orchestration requirements
- **Breaking Changes**: Avoid API contract changes that break existing frontend
- **Testing**: All optimizations must maintain existing test coverage
- **Timeline**: Performance milestone should not block new feature development indefinitely

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Docker Compose for deployment | Simplicity for capstone project and small venues | ✓ Good — Easy deployment |
| PostgreSQL over NoSQL | Relational data (events, requests, votes) fits SQL well | ✓ Good — EF Core works well |
| SignalR for real-time | Native .NET solution, lower complexity than WebSockets | ✓ Good — Real-time works reliably |
| Session-based auth over JWT | Simpler for MVP, less frontend complexity | ✓ Good — Sufficient for v1.0 |

---
*Last updated: 2026-02-05 after Performance Enhancements milestone initialization*
