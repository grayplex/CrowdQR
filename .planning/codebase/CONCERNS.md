# Codebase Concerns

**Analysis Date:** 2026-02-05

## Tech Debt

**Disabled Email Verification Check:**
- Issue: Email verification is disabled in both `DjRoleValidationMiddleware.cs` (lines 64-82) and `AuthService.cs` (lines 106-118). DJ accounts can access protected endpoints without verified emails.
- Files: `D:\CrowdQR/src/CrowdQR.API/Middleware/DjRoleValidationMiddleware.cs`, `D:\CrowdQR/src/CrowdQR.API/Services/AuthService.cs`
- Impact: Reduces account security, allows unverified email addresses to access DJ-only features. Could lead to account takeover through compromised email addresses.
- Fix approach: Uncomment email verification checks, add proper email verification flow to EmailService, implement password reset tokens properly.

**Placeholder Email Service:**
- Issue: `EmailService.cs` (lines 17-72) logs emails to console instead of actually sending them. Verification and password reset emails are not sent.
- Files: `D:\CrowdQR/src/CrowdQR.API/Services/EmailService.cs`
- Impact: Users cannot verify email addresses or reset passwords. Account recovery is broken for production environments.
- Fix approach: Replace placeholder with actual email provider (SendGrid, SMTP, etc.). Maintain test/development logging for CI environments.

**Disabled HTTPS Redirection:**
- Issue: HTTPS redirection is commented out in `Program.cs` (line 229): `// app.UseHttpsRedirection();`
- Files: `D:\CrowdQR/src/CrowdQR.API/Program.cs`
- Impact: Development mode convenience creates security risk in production. Passwords and tokens can be transmitted over HTTP.
- Fix approach: Enable HTTPS redirection in production environment configuration, disable only for development/testing.

## Security Considerations

**Overly Permissive CORS Configuration:**
- Risk: `Program.cs` (lines 116-129) allows all origins: `builder.SetIsOriginAllowed(_ => true)` with `.AllowAnyMethod()` and `.AllowAnyHeader()`.
- Files: `D:\CrowdQR/src/CrowdQR.API/Program.cs`
- Current mitigation: `AllowCredentials()` is enabled, which slightly limits damage
- Recommendations: Replace with explicit origin whitelist. In production, specify only the web app domain(s). Example: `builder.WithOrigins("https://yourdomain.com")`

**Default Hardcoded JWT Secret in Development:**
- Risk: `Program.cs` (lines 53-55) includes a default secret: `"test_jwt_secret_key_that_is_long_enough_for_testing_requirements_12345"`
- Files: `D:\CrowdQR/src/CrowdQR.API/Program.cs`
- Current mitigation: Code checks for 32-character minimum length, prefers environment variables
- Recommendations: Remove hardcoded secret entirely. Fail fast if JWT_SECRET environment variable is not set in non-development environments.

**Unvalidated User ID in Vote Controller:**
- Risk: `VoteController.cs` (line 117) trusts user-supplied UserId in DTO without confirming token claims match.
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/VoteController.cs`
- Current mitigation: Partial check exists: `!User.IsInRole("DJ") && voteDto.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")`
- Recommendations: Always validate that user is voting as themselves or is DJ. Parse claims safely with null coalescing to prevent exceptions.

**Insufficient Authorization Checks on Report Endpoints:**
- Risk: `ReportsController.cs` endpoints accept `djUserId` and `eventId` as route parameters without verifying the requesting user owns those resources.
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/ReportsController.cs`
- Current mitigation: Endpoints may be DJ-only via middleware, but this is not visible in controller code
- Recommendations: Add explicit authorization checks in controller actions to prevent DJs from viewing other DJs' analytics.

**SignalR Hub Allows Unauthenticated Connections:**
- Risk: `CrowdQRHub.cs` (lines 61-66) does not enforce authentication for JoinEvent method. Any client can join any event group.
- Files: `D:\CrowdQR/src/CrowdQR.API/Hubs/CrowdQRHub.cs`
- Current mitigation: None visible
- Recommendations: Add `[Authorize]` attribute to hub methods. Validate that user is allowed to join the specific event.

**Exposed Sensitive Data in Error Messages:**
- Risk: `ExceptionHandlingMiddleware.cs` (line 43) returns `exception.Message` in response: `"detail": exception.Message`
- Files: `D:\CrowdQR/src/CrowdQR.API/Middleware/ExceptionHandlingMiddleware.cs`
- Current mitigation: Wrapped in generic "error occurred" message
- Recommendations: Log full exception server-side, return generic error message to client. Only expose detailed errors in development environment.

**Session Management Uses Server Sessions Without CSRF Protection:**
- Risk: `SessionManager.cs` uses server-side session state without visible CSRF token handling.
- Files: `D:\CrowdQR/src/CrowdQR.Web/Services/SessionManager.cs`
- Current mitigation: Uses AspNetCore.Identity which provides some protection
- Recommendations: Verify CSRF tokens are enforced in forms. Add `[ValidateAntiForgeryToken]` to state-changing operations.

## Performance Bottlenecks

**N+1 Query Pattern in EventController GetEvent:**
- Problem: `EventController.cs` (lines 62-66) loads event with requests and votes, but doesn't optimize vote loading
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/EventController.cs`
- Cause: Uses `.Include(e => e.Requests).ThenInclude(r => r.Votes)` which loads all votes for all requests
- Improvement path: Implement pagination for requests. Use separate endpoint for high-vote-count requests if needed.

**Synchronous Vote Count Calculation:**
- Problem: `VoteController.cs` (lines 165-166) counts all votes for a request after each vote
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/VoteController.cs`
- Cause: No denormalization of vote counts; every request requires COUNT query
- Improvement path: Add `VoteCount` column to Request entity, increment on vote creation instead of recalculating.

**No Query Result Caching:**
- Problem: Controllers repeatedly query same data (e.g., GetVotes in line 38) without caching
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/VoteController.cs`, `D:\CrowdQR/src/CrowdQR.API/Controllers/RequestController.cs`
- Cause: No distributed cache (Redis) or in-memory cache implementation
- Improvement path: Add output caching for GET endpoints. Cache event data with short TTL.

**Unbounded Database Query Results:**
- Problem: `RequestController.cs` (line 38) `GetRequests()` loads all requests without pagination
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/RequestController.cs`
- Cause: No skip/take parameters on queries
- Improvement path: Add pagination parameters (page, pageSize). Set reasonable defaults and limits.

## Fragile Areas

**SignalR Notification Swallows Exceptions:**
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/VoteController.cs` (lines 169-179), `D:\CrowdQR/src/CrowdQR.API/Controllers/SessionController.cs` (lines 289-300)
- Why fragile: Try/catch blocks silently ignore SignalR failures, but main operation continues. Clients don't receive real-time updates without error indication.
- Safe modification: Log errors at WARNING level. Consider adding retry logic for critical notifications. Return response indicating partial success.
- Test coverage: No visible tests for notification failure scenarios.

**Middleware Authorization Logic Hardcoded:**
- Files: `D:\CrowdQR/src/CrowdQR.API/Middleware/DjRoleValidationMiddleware.cs` (lines 87-100)
- Why fragile: Protected paths are hardcoded string array. Adding new DJ-only endpoint requires modifying middleware.
- Safe modification: Use policy-based authorization instead. Define policies in Program.cs, apply via `[Authorize(Policy = "DjOnly")]` attributes.
- Test coverage: Gaps in testing different path combinations.

**Event Slug Uniqueness Not Database Constraint:**
- Files: `D:\CrowdQR/src/CrowdQR.API/Data/CrowdQRContext.cs` (line 71), `D:\CrowdQR/src/CrowdQR.API/Controllers/EventController.cs` (lines 234-238)
- Why fragile: Uniqueness checked in application code with race condition window. Two concurrent requests could create duplicate slugs.
- Safe modification: Ensure database has unique constraint: `.HasIndex(e => e.Slug).IsUnique()` is declared (already present), but add database-level enforcement in migration.
- Test coverage: No concurrent write tests for slug collisions.

**Hard-coded SignalR Message Names:**
- Files: `D:\CrowdQR/src/CrowdQR.API/Hubs/CrowdQRHub.cs` (lines 50, 64, 76)
- Why fragile: Message names like `"userLeftEvent"`, `"userJoinedEvent"` are strings, not strongly typed. Client code can break silently.
- Safe modification: Extract to constants. Consider code generation from shared interface.
- Test coverage: Integration tests needed for message payload formats.

## Known Issues

**Audience User Creation Implicit on First Login:**
- Symptoms: Any username automatically creates an "Audience" user account on first login without password
- Files: `D:\CrowdQR/src/CrowdQR.API/Services/AuthService.cs` (lines 56-79)
- Trigger: Call AuthenticateUser with username and no password, username not in database
- Workaround: None; this is intended behavior for audience members but bypasses registration flow
- Impact: Could allow account enumeration if login attempts are tracked by username.

**Email Verification Token Not Used in Verification:**
- Symptoms: Email verification token is generated and stored but there's no endpoint to use it
- Files: `D:\CrowdQR/src/CrowdQR.API/Services/AuthService.cs` (lines 183-185), `D:\CrowdQR/src/CrowdQR.API/Controllers/AuthController.cs` (lines 120-141)
- Trigger: Register DJ, token is created but EmailService doesn't send it
- Workaround: Check database directly for EmailVerificationToken value, use in URL manually
- Impact: Email verification flow is completely broken.

**No Refresh Token Mechanism:**
- Symptoms: JWT tokens expire (24 hours) with no way to extend session without re-login
- Files: `D:\CrowdQR/src/CrowdQR.API/Services/TokenService.cs`
- Trigger: Wait for JWT expiration time
- Workaround: Re-login to get new token
- Impact: Users logged out during long event sessions.

## Missing Critical Features

**Audience Password Protection:**
- Problem: Audience users have no password; they can't secure their accounts against unauthorized access
- Blocks: Account recovery, preventing account hijacking for voting/requesting songs
- Recommendation: Make email and password optional for audience members OR require password for accessing existing account.

**Event Access Control:**
- Problem: Any user can join any event. No concept of invite-only or password-protected events.
- Blocks: Private events, limiting participation to selected users
- Recommendation: Add EventAccessType enum (Public/Private/PasswordProtected). Validate user access in SessionController.

**Rate Limiting:**
- Problem: No rate limiting on requests, votes, or login attempts.
- Blocks: Preventing spam, DDoS attacks, brute force attacks
- Recommendation: Use AspNetCore.RateLimiting for endpoints. Implement per-user and per-IP limits.

**Database Backups and Recovery:**
- Problem: No visible backup strategy or disaster recovery plan
- Blocks: Data loss recovery, compliance with data protection regulations
- Recommendation: Document backup procedures. Test recovery process regularly.

## Test Coverage Gaps

**Authorization Bypass Scenarios:**
- What's not tested: Cross-user access, DJ role elevation, admin endpoint access without proper claims
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/EventController.cs`, `D:\CrowdQR/src/CrowdQR.API/Controllers/UserController.cs`
- Risk: Privilege escalation vulnerabilities could exist without integration tests catching them
- Priority: **High** - Security critical

**Database Constraint Violations:**
- What's not tested: Unique constraint violations (slug, one_vote_per_user, one_session_per_user_event)
- Files: `D:\CrowdQR/src/CrowdQR.API/Data/CrowdQRContext.cs` (lines 62, 107-108, 129-130)
- Risk: Race conditions could create duplicate data silently
- Priority: **High** - Data integrity

**SignalR Connection Failures:**
- What's not tested: SignalR hub disconnection, reconnection logic, message delivery failures
- Files: `D:\CrowdQR/src/CrowdQR.API/Hubs/CrowdQRHub.cs`
- Risk: Real-time features fail silently, users miss updates
- Priority: **Medium** - User experience impact

**Email Verification Flow:**
- What's not tested: Email token generation, expiry validation, re-send logic
- Files: `D:\CrowdQR/src/CrowdQR.API/Services/AuthService.cs`, `D:\CrowdQR/src/CrowdQR.API/Controllers/AuthController.cs`
- Risk: Email verification feature is completely broken but untested
- Priority: **High** - Feature broken

**Concurrent Request/Vote Operations:**
- What's not tested: Two users voting for same request simultaneously, duplicate vote prevention under race conditions
- Files: `D:\CrowdQR/src/CrowdQR.API/Controllers/VoteController.cs` (lines 144-151)
- Risk: Duplicate votes possible despite database constraint due to application-level checks
- Priority: **High** - Data integrity

---

*Concerns audit: 2026-02-05*
