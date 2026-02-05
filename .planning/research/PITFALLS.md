# Pitfalls Research: Performance Optimization

**Domain:** ASP.NET Core Performance Enhancements (Subsequent Milestone)
**Researched:** 2026-02-05
**Confidence:** HIGH

## Critical Pitfalls

### Pitfall 1: Async-over-Sync Conversion Without Understanding Context

**What goes wrong:**
Converting synchronous code to async by blindly replacing `.Result` with `await` without understanding thread pool behavior leads to deadlocks, thread pool starvation, and worse performance than the original synchronous code.

**Why it happens:**
Developers hear "async is better" and mechanically convert code without understanding that:
- ASP.NET Core's synchronization context differs from ASP.NET Framework
- Blocking async code (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) causes thread pool exhaustion
- Using `Task.Run()` to "make code async" creates unnecessary scheduling overhead
- Not all I/O operations benefit from async (tiny operations may have more overhead than benefit)

**How to avoid:**
1. **Go async all the way** - Never mix blocking calls with async code:
   ```csharp
   // WRONG: Blocks thread pool
   public IActionResult Bad()
   {
       var data = _service.GetDataAsync().Result;
       return Ok(data);
   }

   // RIGHT: Async all the way
   public async Task<IActionResult> Good()
   {
       var data = await _service.GetDataAsync();
       return Ok(data);
   }
   ```

2. **Never use `async void`** - Always return `Task` in ASP.NET Core:
   ```csharp
   // WRONG: HttpContext disposed after first await
   public async void Bad()
   {
       await Task.Delay(1000);
       await Response.WriteAsync("Hello"); // Crash!
   }

   // RIGHT: Return Task
   public async Task<IActionResult> Good()
   {
       await Task.Delay(1000);
       return Ok("Hello");
   }
   ```

3. **Don't wrap sync in Task.Run** unless doing CPU-bound work in background:
   ```csharp
   // WRONG: Unnecessary overhead
   public async Task<IActionResult> Bad()
   {
       var result = await Task.Run(() => _service.GetData());
       return Ok(result);
   }

   // RIGHT: If sync is required, just call it
   public IActionResult Better()
   {
       var result = _service.GetData();
       return Ok(result);
   }

   // BEST: Make the service method async
   public async Task<IActionResult> Best()
   {
       var result = await _service.GetDataAsync();
       return Ok(result);
   }
   ```

4. **Use async versions of framework methods**:
   - `Request.ReadFormAsync()` not `Request.Form` (sync over async)
   - `JsonSerializer.DeserializeAsync()` not `JsonSerializer.Deserialize()`
   - `StreamReader.ReadToEndAsync()` not `StreamReader.ReadToEnd()`
   - `context.SaveChangesAsync()` not `context.SaveChanges()`

**Warning signs:**
- Thread pool starvation under load (many requests queued)
- High CPU usage but low throughput
- Requests timing out that worked fine at low concurrency
- `ThreadPool.GetAvailableThreads()` shows near-zero available threads
- Profiler shows threads blocked on `.Result` or `.Wait()`

**Phase to address:**
**Phase 1: Foundation** - Establish async patterns BEFORE adding caching/optimization. Audit all controllers and services for blocking calls. Fix async anti-patterns first, then optimize.

---

### Pitfall 2: Cache Stampede (Thundering Herd) on Popular Keys

**What goes wrong:**
When a popular cache entry expires, hundreds of concurrent requests all miss the cache simultaneously, each triggering the expensive database query or API call. This causes:
- Database connection pool exhaustion
- Database CPU spike (same query executed 500+ times)
- Request timeout cascade
- Worse performance than no caching at all

**Why it happens:**
Standard cache-aside pattern has no coordination between requests:
```csharp
// VULNERABLE: Multiple threads all miss and fetch
var cached = await _cache.GetAsync(key);
if (cached == null)
{
    var data = await ExpensiveDbQuery(); // 500 requests do this!
    await _cache.SetAsync(key, data);
}
```

When the cache entry expires at 14:00:00:
- 14:00:00.001 - Request A misses cache, starts query
- 14:00:00.002 - Request B misses cache, starts query
- 14:00:00.003 - Request C misses cache, starts query
- ... (498 more requests)
- All hit database simultaneously

**How to avoid:**
1. **Use HybridCache with built-in stampede protection** (ASP.NET Core 9.0+):
   ```csharp
   // RIGHT: Only one request fetches data, others wait
   return await _hybridCache.GetOrCreateAsync(
       $"event:{eventId}",
       async cancel => await _context.Events
           .Include(e => e.Requests)
           .FirstOrDefaultAsync(e => e.Id == eventId, cancel),
       options: new HybridCacheEntryOptions
       {
           Expiration = TimeSpan.FromMinutes(5)
       },
       cancellationToken: cancellationToken
   );
   ```

2. **If using IDistributedCache, implement manual locking**:
   ```csharp
   private static readonly SemaphoreSlim _lock = new(1, 1);

   public async Task<Event> GetEventAsync(int eventId)
   {
       var cacheKey = $"event:{eventId}";
       var cached = await _cache.GetStringAsync(cacheKey);

       if (cached != null)
           return JsonSerializer.Deserialize<Event>(cached);

       // Lock prevents stampede
       await _lock.WaitAsync();
       try
       {
           // Double-check after acquiring lock
           cached = await _cache.GetStringAsync(cacheKey);
           if (cached != null)
               return JsonSerializer.Deserialize<Event>(cached);

           var data = await _context.Events
               .Include(e => e.Requests)
               .FirstOrDefaultAsync(e => e.Id == eventId);

           await _cache.SetStringAsync(cacheKey,
               JsonSerializer.Serialize(data),
               new DistributedCacheEntryOptions
               {
                   AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
               });

           return data;
       }
       finally
       {
           _lock.Release();
       }
   }
   ```

3. **Use staggered TTLs** to prevent all keys expiring simultaneously:
   ```csharp
   var ttl = TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
   ```

4. **Implement cache warming** for critical data:
   ```csharp
   // Background service pre-populates cache before expiration
   public class CacheWarmerService : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               await WarmCriticalCaches();
               await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
           }
       }
   }
   ```

**Warning signs:**
- Periodic spikes in database CPU usage (every 5 minutes when cache expires)
- Connection pool exhaustion errors at regular intervals
- SQL query logs show same query executed many times simultaneously
- Response time spikes at cache TTL boundaries
- APM shows database wait time spikes

**Phase to address:**
**Phase 2: Caching Implementation** - Address stampede protection DURING cache implementation, not after. Use HybridCache or implement locking from day one.

---

### Pitfall 3: N+1 Queries Persist After "Optimization"

**What goes wrong:**
Developers add `.Include()` statements to fix N+1 queries, but the problem persists or moves to different relationships. They verify fix in debugger with single request, but production with 100 concurrent users shows same symptoms.

**Why it happens:**
Entity Framework Core has multiple N+1 scenarios that aren't obvious:
1. **Implicit loading in loops** (even with explicit loading configured)
2. **Projection after materialization** (ToList then Select)
3. **Multiple navigation levels** (Include doesn't cascade automatically)
4. **Lazy loading silently enabled** via proxies or injection
5. **Missing includes in related services** that query the same entities

CrowdQR-specific example (EventController):
```csharp
// WRONG: Looks optimized but still has N+1
public async Task<IActionResult> GetEvent(int id)
{
    var evt = await _context.Events
        .Include(e => e.Requests) // Loads requests
        .FirstOrDefaultAsync(e => e.Id == id);

    // N+1 HERE: Each request triggers query for vote count
    foreach (var request in evt.Requests)
    {
        request.VoteCount = _context.Votes
            .Count(v => v.RequestId == request.Id); // NEW QUERY EACH!
    }

    return Ok(evt);
}
```

**How to avoid:**
1. **Enable sensitive data logging and log query counts in development**:
   ```csharp
   // Program.cs
   builder.Services.AddDbContext<CrowdQRContext>(options => {
       options.UseNpgsql(connectionString);
       if (builder.Environment.IsDevelopment())
       {
           options.EnableSensitiveDataLogging();
           options.LogTo(Console.WriteLine, LogLevel.Information);
       }
   });
   ```

2. **Use single queries with projections**:
   ```csharp
   // RIGHT: Single query with computed values
   public async Task<IActionResult> GetEvent(int id)
   {
       var evt = await _context.Events
           .Where(e => e.Id == id)
           .Select(e => new EventDto
           {
               Id = e.Id,
               Name = e.Name,
               Requests = e.Requests.Select(r => new RequestDto
               {
                   Id = r.Id,
                   SongName = r.SongName,
                   Artist = r.Artist,
                   VoteCount = r.Votes.Count() // Computed in query
               }).ToList()
           })
           .FirstOrDefaultAsync();

       return Ok(evt);
   }
   ```

3. **Use AsSplitQuery for large includes** (CrowdQR has many votes per request):
   ```csharp
   // For large collections, split into multiple queries
   var evt = await _context.Events
       .Include(e => e.Requests)
           .ThenInclude(r => r.Votes)
       .AsSplitQuery() // Prevents cartesian explosion
       .FirstOrDefaultAsync(e => e.Id == id);
   ```

4. **Always use AsNoTracking for read-only queries**:
   ```csharp
   // CrowdQR API returns DTOs, doesn't need tracking
   var events = await _context.Events
       .AsNoTracking() // 40-50% faster for read operations
       .Include(e => e.Requests)
       .ToListAsync();
   ```

5. **Create integration test that counts queries**:
   ```csharp
   [Fact]
   public async Task GetEvent_Should_Execute_Two_Queries_Maximum()
   {
       var queryCount = 0;
       _context.Database.Log += sql => queryCount++;

       var result = await _controller.GetEvent(1);

       Assert.True(queryCount <= 2, $"Expected ≤2 queries, got {queryCount}");
   }
   ```

**Warning signs:**
- Database CPU high but no slow queries in APM
- Query logs show same query with different IDs executed repeatedly
- Adding indexes doesn't improve performance
- Response time scales linearly with record count (100 records = 10x slower than 10 records)
- `dotnet-counters` shows high database round-trip count
- EF Core logging shows hundreds of queries for single request

**Phase to address:**
**Phase 3: Database Optimization** - Audit ALL queries with logging enabled. Test with realistic data volumes (100+ requests per event). Create automated query count tests before optimization.

---

### Pitfall 4: Cache Invalidation Bugs Cause Stale Data in Production

**What goes wrong:**
Cached data becomes stale after updates, causing:
- Users see old vote counts (frustrating in real-time app like CrowdQR)
- DJs approve requests but they stay in "pending" list
- Event updates (name, status) don't appear until cache expires
- Users report "the app is broken" but refreshing after 5 minutes "fixes" it

**Why it happens:**
Developers implement caching but forget to invalidate when data changes:
```csharp
// WRONG: Cache is set but never invalidated
[HttpPost("vote")]
public async Task<IActionResult> Vote(int requestId)
{
    var vote = new Vote { RequestId = requestId, UserId = GetUserId() };
    _context.Votes.Add(vote);
    await _context.SaveChangesAsync();

    return Ok(); // Cache still has old vote count!
}

[HttpGet("event/{id}")]
public async Task<IActionResult> GetEvent(int id)
{
    return await _cache.GetOrCreateAsync($"event:{id}", async () =>
        await _context.Events.Include(e => e.Requests).FirstAsync(e => e.Id == id)
    );
}
```

**How to avoid:**
1. **Invalidate cache immediately after writes**:
   ```csharp
   [HttpPost("vote")]
   public async Task<IActionResult> Vote(int requestId)
   {
       var vote = new Vote { RequestId = requestId, UserId = GetUserId() };
       _context.Votes.Add(vote);
       await _context.SaveChangesAsync();

       // Invalidate event cache that includes this request
       var request = await _context.Requests
           .AsNoTracking()
           .FirstAsync(r => r.Id == requestId);
       await _cache.RemoveAsync($"event:{request.EventId}");

       return Ok();
   }
   ```

2. **Create invalidation service to centralize logic**:
   ```csharp
   public class CacheInvalidationService
   {
       private readonly IDistributedCache _cache;

       public async Task InvalidateEventAsync(int eventId)
       {
           await _cache.RemoveAsync($"event:{eventId}");
           await _cache.RemoveAsync($"event:requests:{eventId}");
           await _cache.RemoveAsync($"dashboard:{eventId}");
       }

       public async Task InvalidateRequestAsync(int requestId)
       {
           var request = await _context.Requests
               .AsNoTracking()
               .Select(r => new { r.EventId })
               .FirstAsync(r => r.Id == requestId);

           await InvalidateEventAsync(request.EventId);
       }
   }
   ```

3. **Use tag-based invalidation** (if using Redis with extensions):
   ```csharp
   // When caching, add tags
   await _cache.SetAsync($"event:{id}", data, new DistributedCacheEntryOptions
   {
       AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
       Tags = new[] { $"event:{id}", "events" }
   });

   // Invalidate by tag
   await _cache.RemoveByTagAsync($"event:{id}"); // Removes all related cache entries
   ```

4. **Use short TTLs for frequently changing data**:
   ```csharp
   // Vote counts change rapidly in CrowdQR
   var options = new DistributedCacheEntryOptions
   {
       // Short TTL for real-time data
       AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
       // Longer sliding window if accessed frequently
       SlidingExpiration = TimeSpan.FromSeconds(10)
   };
   ```

5. **Combine cache with SignalR for real-time updates**:
   ```csharp
   // After vote, invalidate cache AND push update via SignalR
   [HttpPost("vote")]
   public async Task<IActionResult> Vote(int requestId)
   {
       var vote = new Vote { RequestId = requestId, UserId = GetUserId() };
       _context.Votes.Add(vote);
       await _context.SaveChangesAsync();

       // Invalidate cache
       await _cacheInvalidation.InvalidateRequestAsync(requestId);

       // Push real-time update (CrowdQR already has this)
       await _hubNotification.NotifyVoteUpdate(requestId);

       return Ok();
   }
   ```

6. **Add cache version to keys** for instant global invalidation:
   ```csharp
   private static int _cacheVersion = 1;

   private string GetCacheKey(string key) => $"v{_cacheVersion}:{key}";

   public void InvalidateAllCache()
   {
       Interlocked.Increment(ref _cacheVersion); // All old keys now orphaned
   }
   ```

**Warning signs:**
- Users report seeing old data, fixed by "hard refresh"
- Vote counts are inconsistent between different views
- Admin changes take minutes to appear
- Manual cache clearing "fixes" user-reported bugs
- Integration tests pass but E2E tests fail
- More reports during high-activity periods (cache hit rate is high)

**Phase to address:**
**Phase 2: Caching Implementation** - Build invalidation logic ALONGSIDE caching. Never cache without invalidation strategy. Test invalidation in integration tests.

---

### Pitfall 5: Over-Optimization Creates Unmaintainable Code

**What goes wrong:**
Developers optimize code based on assumptions rather than measurements, resulting in:
- Complex, hard-to-read code that offers <5% performance improvement
- Premature object pooling that causes memory leaks
- Manual memory management in managed language
- Micro-optimizations that break during framework updates
- Team velocity drops because code is hard to modify

Example:
```csharp
// OVER-OPTIMIZED: Complex, hard to maintain
private static readonly ObjectPool<StringBuilder> _stringBuilderPool = ObjectPool.Create<StringBuilder>();

public string BuildEventDescription(Event evt)
{
    var sb = _stringBuilderPool.Get();
    try
    {
        sb.Clear();
        sb.Append(evt.Name);
        sb.Append(" - ");
        sb.Append(evt.Host.UserName);
        sb.Append(" (");
        sb.Append(evt.Requests.Count);
        sb.Append(" requests)");
        return sb.ToString();
    }
    finally
    {
        _stringBuilderPool.Return(sb);
    }
}

// SIMPLE: 2ms slower, infinitely more maintainable
public string BuildEventDescription(Event evt)
{
    return $"{evt.Name} - {evt.Host.UserName} ({evt.Requests.Count} requests)";
}
```

**Why it happens:**
- Developers read about optimization techniques and apply them everywhere
- "Best practices" articles recommend techniques without context
- Lack of profiling data to prove optimization is needed
- Cargo cult programming: "Big tech companies do it, so we should too"
- Misunderstanding Knuth's quote about premature optimization

**How to avoid:**
1. **Profile FIRST, optimize SECOND**:
   ```bash
   # Use dotnet-trace to collect production profile
   dotnet-trace collect --process-id [pid] --profile cpu-sampling

   # Analyze with PerfView, Visual Studio, or speedscope
   ```

2. **Establish performance baseline and targets**:
   ```csharp
   // Create performance test with actual measurements
   [Benchmark]
   public async Task GetEvent_Baseline()
   {
       var result = await _controller.GetEvent(1);
       // BenchmarkDotNet measures actual time
   }
   ```

3. **Only optimize hot paths** (code executed on every request or thousands of times):
   - Middleware pipeline (early middleware affects ALL requests)
   - GetEvent endpoint (called every 2-3 seconds by audience)
   - Vote endpoint (high frequency during busy periods)
   - SignalR broadcasts (sent to hundreds of connections)

   **Don't optimize**:
   - Event creation (happens once per gig)
   - User registration (happens once per user)
   - Admin dashboard (low frequency, single user)

4. **Use "good enough" optimizations**:
   ```csharp
   // GOOD ENOUGH: AsNoTracking + projection
   var events = await _context.Events
       .AsNoTracking()
       .Select(e => new EventDto
       {
           Id = e.Id,
           Name = e.Name,
           RequestCount = e.Requests.Count
       })
       .ToListAsync();

   // OVER-OPTIMIZED: Raw SQL with manual mapping
   var events = await _context.Database
       .SqlQueryRaw<EventDto>(@"
           SELECT e.id, e.name, COUNT(r.id) as request_count
           FROM events e
           LEFT JOIN requests r ON r.event_id = e.id
           GROUP BY e.id, e.name
       ")
       .ToListAsync();
   // Marginally faster, much harder to maintain
   ```

5. **Set performance budgets based on user experience**:
   - API endpoints: <100ms p95, <250ms p99 (good enough for CrowdQR)
   - Database queries: <50ms p95
   - SignalR broadcast: <500ms to reach all clients

   If you hit budgets, stop optimizing. Ship features instead.

6. **Use BenchmarkDotNet to validate optimizations**:
   ```csharp
   [MemoryDiagnoser]
   public class OptimizationBenchmark
   {
       [Benchmark(Baseline = true)]
       public string Original() => BuildDescriptionOriginal();

       [Benchmark]
       public string Optimized() => BuildDescriptionOptimized();
   }
   ```

   Only keep optimization if:
   - ≥20% performance improvement, OR
   - ≥50% memory reduction
   - AND code complexity increase is minimal

**Warning signs:**
- Pull requests have "optimization" commits with no benchmarks
- Code reviews take longer due to complexity
- New developers struggle to understand "optimized" code
- Bugs introduced in heavily optimized sections
- Performance hasn't improved despite "optimizations"
- You're using `unsafe` code or pointer arithmetic
- Manual memory pooling for small objects

**Phase to address:**
**Phase 1: Foundation** - Establish profiling and benchmarking BEFORE optimization. Create performance budgets. Document hot paths. Reject optimizations without measurements.

---

### Pitfall 6: APM Overhead Degrades Performance in Production

**What goes wrong:**
Adding APM (Application Performance Monitoring) causes the very performance problems it's meant to detect:
- Response times increase by 100-500ms
- CPU usage increases by 20-40%
- Memory consumption doubles
- Network bandwidth consumed by telemetry
- Distributed tracing overhead compounds across services

**Why it happens:**
APM tools instrument EVERY method call, log EVERY request, and capture EVERY exception:
```csharp
// APM instruments this entire chain
[HttpGet("event/{id}")]
public async Task<IActionResult> GetEvent(int id)
{
    // Span 1: Controller method
    var evt = await _service.GetEventAsync(id);
    // Span 2: Service method
    //   Span 3: Database query
    //   Span 4: Cache lookup
    //   Span 5: Serialization
    return Ok(evt);
}
// Each span adds 5-20ms overhead
```

**How to avoid:**
1. **Use sampling, not full instrumentation**:
   ```csharp
   // OpenTelemetry configuration
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing =>
       {
           tracing.AddAspNetCoreInstrumentation(options =>
           {
               // Only trace 10% of requests
               options.Filter = (httpContext) =>
               {
                   return Random.Shared.NextDouble() < 0.1;
               };
           });
           tracing.AddEntityFrameworkCoreInstrumentation(options =>
           {
               // Don't trace every query
               options.SetDbStatementForText = false;
               options.SetDbStatementForStoredProcedure = false;
           });
       });
   ```

2. **Use head-based sampling for high-traffic endpoints**:
   ```csharp
   // Instrument errors always, success sometimes
   options.Filter = (httpContext) =>
   {
       // Always trace errors
       if (httpContext.Response.StatusCode >= 400)
           return true;

       // Always trace admin endpoints (low traffic)
       if (httpContext.Request.Path.StartsWithSegments("/admin"))
           return true;

       // Sample 5% of audience requests (high traffic)
       if (httpContext.Request.Path.StartsWithSegments("/event"))
           return Random.Shared.NextDouble() < 0.05;

       // Default: 10% sampling
       return Random.Shared.NextDouble() < 0.1;
   };
   ```

3. **Disable expensive instrumentation in production**:
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing =>
       {
           tracing.AddEntityFrameworkCoreInstrumentation(options =>
           {
               // Don't capture full SQL statements (expensive)
               options.SetDbStatementForText = false;
               options.SetDbStatementForStoredProcedure = false;

               // Don't capture query parameters (PII risk + overhead)
               options.EnrichWithIDbCommand = null;
           });
       });
   ```

4. **Use metrics instead of traces for performance monitoring**:
   ```csharp
   // Metrics have ~1000x less overhead than traces
   public class EventController : ControllerBase
   {
       private static readonly Histogram<double> _requestDuration =
           Meter.CreateHistogram<double>("http.server.request.duration");

       [HttpGet("event/{id}")]
       public async Task<IActionResult> GetEvent(int id)
       {
           var sw = Stopwatch.StartNew();
           try
           {
               var evt = await _service.GetEventAsync(id);
               return Ok(evt);
           }
           finally
           {
               _requestDuration.Record(sw.Elapsed.TotalSeconds,
                   new KeyValuePair<string, object>("endpoint", "GetEvent"));
           }
       }
   }
   ```

5. **Use tail-based sampling for production** (requires APM backend support):
   ```csharp
   // Only keep traces that are slow or have errors
   // Requires backend like Jaeger, Tempo, or commercial APM
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing =>
       {
           tracing.SetSampler(new TraceIdRatioBasedSampler(1.0)); // Capture all
           // Backend drops normal fast requests, keeps slow/error traces
       });
   ```

6. **Measure APM overhead before production deployment**:
   ```bash
   # Load test WITHOUT APM
   k6 run --vus 100 --duration 5m load-test.js
   # Record: p95=45ms, p99=78ms, CPU=35%

   # Load test WITH APM
   k6 run --vus 100 --duration 5m load-test.js
   # Record: p95=58ms, p99=112ms, CPU=48%

   # Overhead: +28% latency, +37% CPU
   # Decision: Use 10% sampling to reduce overhead to <5%
   ```

**Warning signs:**
- Response times increased after APM deployment
- CPU usage higher in production than load testing
- Network bandwidth usage is surprisingly high
- APM agent process consuming significant memory
- Application logs show APM errors or warnings
- Response time p99 is 2x higher than p50 (long tail from instrumentation)

**Phase to address:**
**Phase 5: Monitoring Implementation** - Configure sampling BEFORE production. Load test WITH APM enabled. Budget for 5-10% overhead. Start with 10% sampling, increase only if needed.

---

### Pitfall 7: Database Connection Pool Exhaustion After Async Conversion

**What goes wrong:**
After converting sync code to async, the application starts throwing:
- "Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool."
- "The connection pool has been exhausted"
- Works fine at 10 concurrent users, crashes at 50 users

**Why it happens:**
1. **Connection leaks** - Async code makes it easier to forget cleanup:
   ```csharp
   // WRONG: Connection leaked if exception before Dispose
   public async Task<Event> GetEventAsync(int id)
   {
       var connection = new NpgsqlConnection(_connectionString);
       await connection.OpenAsync();

       var command = new NpgsqlCommand("SELECT * FROM events WHERE id = @id", connection);
       command.Parameters.AddWithValue("@id", id);

       var reader = await command.ExecuteReaderAsync();
       // If exception here, connection never closed!

       await connection.DisposeAsync();
   }
   ```

2. **Long-running async operations hold connections**:
   ```csharp
   // WRONG: Connection held during slow external API call
   public async Task ProcessRequestAsync(int requestId)
   {
       using var connection = _context.Database.GetDbConnection();
       await connection.OpenAsync();

       var request = await _context.Requests.FindAsync(requestId);
       // Connection held open during API call!
       var songInfo = await _spotifyApi.GetSongInfoAsync(request.SongName);

       request.ExternalId = songInfo.Id;
       await _context.SaveChangesAsync();
       // Connection held for 500ms+ when query only needed 10ms
   }
   ```

3. **Background tasks using scoped DbContext**:
   ```csharp
   // WRONG: Scoped context disposed after request, background task fails
   [HttpPost("request")]
   public async Task<IActionResult> CreateRequest(RequestDto dto)
   {
       var request = new Request { SongName = dto.SongName };
       _context.Requests.Add(request);
       await _context.SaveChangesAsync();

       // Background task tries to use disposed context
       _ = Task.Run(async () =>
       {
           await Task.Delay(5000);
           _context.Requests.Update(request); // ObjectDisposedException!
       });

       return Ok();
   }
   ```

**How to avoid:**
1. **Always use `using` or `await using` with DbContext**:
   ```csharp
   // RIGHT: Connection guaranteed to be released
   public async Task<Event> GetEventAsync(int id)
   {
       await using var context = _contextFactory.CreateDbContext();
       return await context.Events
           .Include(e => e.Requests)
           .FirstOrDefaultAsync(e => e.Id == id);
   }
   ```

2. **Configure connection pool limits appropriately**:
   ```csharp
   // Program.cs
   var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

   builder.Services.AddDbContext<CrowdQRContext>(options =>
   {
       options.UseNpgsql(connectionString, npgsqlOptions =>
       {
           // Default is 100, but set explicitly
           npgsqlOptions.MaxPoolSize(100);
           // Minimum connections kept alive
           npgsqlOptions.MinPoolSize(10);
           // Connection lifetime (recycle after 15 min)
           npgsqlOptions.ConnectionIdleLifetime(TimeSpan.FromMinutes(15));
           // Command timeout
           npgsqlOptions.CommandTimeout(30);
       });
   });
   ```

3. **Use IDbContextFactory for background tasks**:
   ```csharp
   public class RequestProcessor : BackgroundService
   {
       private readonly IDbContextFactory<CrowdQRContext> _contextFactory;

       public RequestProcessor(IDbContextFactory<CrowdQRContext> contextFactory)
       {
           _contextFactory = contextFactory;
       }

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               // Create new context for each batch
               await using var context = await _contextFactory.CreateDbContextAsync(stoppingToken);

               var requests = await context.Requests
                   .Where(r => r.Status == RequestStatus.Pending)
                   .Take(10)
                   .ToListAsync(stoppingToken);

               // Process requests...

               await context.SaveChangesAsync(stoppingToken);
               // Context disposed, connection returned to pool

               await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
           }
       }
   }
   ```

4. **Don't hold connections during external calls**:
   ```csharp
   // RIGHT: Load data, release connection, then do external work
   public async Task ProcessRequestAsync(int requestId)
   {
       // Load data
       Request request;
       await using (var context = _contextFactory.CreateDbContext())
       {
           request = await context.Requests.FindAsync(requestId);
       }
       // Connection released here

       // Do slow external work
       var songInfo = await _spotifyApi.GetSongInfoAsync(request.SongName);

       // Re-acquire connection for update
       await using (var context = _contextFactory.CreateDbContext())
       {
           context.Requests.Attach(request);
           request.ExternalId = songInfo.Id;
           await context.SaveChangesAsync();
       }
       // Connection released immediately
   }
   ```

5. **Monitor connection pool metrics**:
   ```csharp
   // Add health check for connection pool
   builder.Services.AddHealthChecks()
       .AddNpgSql(
           connectionString: connectionString,
           healthQuery: "SELECT 1",
           name: "postgres",
           failureStatus: HealthStatus.Unhealthy,
           tags: new[] { "db", "sql", "postgres" }
       );

   // Add custom metric for pool size
   public class ConnectionPoolMetrics : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           var poolSize = Meter.CreateObservableGauge("db.pool.size",
               () => NpgsqlConnection.ClearPool()); // Get current pool size

           while (!stoppingToken.IsCancellationRequested)
           {
               await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
           }
       }
   }
   ```

**Warning signs:**
- "Connection pool exhausted" errors in logs
- Response times increase linearly with concurrent users
- Works in development, fails in production
- Health checks fail intermittently
- PostgreSQL shows fewer connections than pool size (leaked connections)
- Errors occur after long-running background tasks
- Connection errors increase after deployments

**Phase to address:**
**Phase 1: Foundation** - Fix async patterns AND connection management together. Audit all DbContext usage. Add connection pool monitoring. Load test to verify pool sizing.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| **Cache without invalidation** | Fast initial implementation | Stale data bugs, user complaints, emergency cache clears | NEVER - always implement invalidation |
| **Manual IDistributedCache instead of HybridCache** | Works on ASP.NET Core 8.0 | Stampede bugs, complex serialization code | Only if using .NET 7/8 (upgrade to .NET 9 for HybridCache) |
| **Blocking async calls with .Result** | Avoids async/await refactor | Thread pool exhaustion, production crashes | NEVER in ASP.NET Core |
| **Lazy loading enabled** | Convenient, less Include() code | Hidden N+1 queries, unpredictable performance | NEVER - use explicit loading |
| **No query logging in development** | Cleaner console output | N+1 queries go undetected until production | NEVER - always log queries during development |
| **100% APM instrumentation** | Complete observability | 20-40% performance overhead | Only in staging/pre-prod, use sampling in production |
| **No connection pool limits** | "Infinite" scaling | Database overwhelmed, cascading failures | NEVER - always set MaxPoolSize |
| **Object pooling for small objects** | Micro-optimization | Complex code, potential leaks | Only for objects >85KB (LOH) with proven benefit |
| **Raw SQL instead of LINQ** | Slightly faster queries | SQL injection risk, hard to maintain | Only for proven bottlenecks after LINQ optimization |
| **In-memory cache in server farm** | Easy to implement | Data inconsistency across servers | Only with sticky sessions, otherwise use distributed cache |
| **No load testing before production** | Faster time to market | Performance surprises in production | NEVER for performance milestone - load test is critical |

---

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| **Redis** | Not handling connection failures gracefully | Wrap cache calls in try-catch, fail open to database: `try { return await _cache.Get(); } catch { return await _db.Get(); }` |
| **PostgreSQL** | Using synchronous Npgsql methods | Use async: `ExecuteReaderAsync`, `ExecuteNonQueryAsync` |
| **SignalR** | Broadcasting to all clients on every change | Use groups: `Clients.Group(eventId).SendAsync()` to reduce broadcast overhead |
| **SignalR** | Not checking connection state before broadcast | Check `HubContext.Clients` null, handle disconnections gracefully |
| **Redis** | Using connection string without SSL in production | Enable SSL: `"redis-server:6379,ssl=true,abortConnect=false"` |
| **PostgreSQL** | Hardcoded connection string with max pool size | Configure in appsettings: `"Max Pool Size=100;Min Pool Size=10"` |
| **APM** | Sending PII in traces (SQL parameters, user data) | Sanitize: `options.SetDbStatementForText = false`, filter sensitive tags |
| **Health checks** | Expensive health check queries (full table scan) | Use lightweight: `SELECT 1` for database, `/health/live` for dependencies |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| **No pagination** | API returns all requests for event | Add pagination: `.Skip().Take()`, return total count | >100 requests per event |
| **SignalR broadcasting full objects** | High bandwidth usage, slow updates | Broadcast only changes: `{ requestId: 123, voteCount: 45 }` instead of full event | >50 concurrent clients |
| **Synchronous vote counting** | Slow API responses | Make async: `await _context.Votes.CountAsync()` | >500 votes per event |
| **No database indexes** | Slow queries despite caching | Add indexes on foreign keys, frequently queried columns | >1000 records per table |
| **Tracking on read-only queries** | High memory usage | Use `AsNoTracking()` for read-only | >100 entities per query |
| **Cartesian explosion in Include** | Massive result sets, slow queries | Use `AsSplitQuery()` or projection | Event with >50 requests, each with >20 votes |
| **JSON serialization on every request** | CPU spikes during busy periods | Cache serialized JSON, not objects | >100 requests/sec |
| **No response compression** | High bandwidth, slow mobile | Enable Brotli/Gzip compression | Responses >10KB |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Caching:** Cache invalidation implemented and tested (not just cache reading)
- [ ] **Caching:** Stampede protection verified under load (concurrent requests don't trigger duplicate queries)
- [ ] **Async conversion:** No blocking calls remain (audit all `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`)
- [ ] **Async conversion:** No `async void` methods (all return `Task` or `Task<T>`)
- [ ] **Database queries:** N+1 queries eliminated (verify with query logging in dev)
- [ ] **Database queries:** AsNoTracking() used for read-only (verify memory usage doesn't grow)
- [ ] **APM:** Sampling configured (not 100% instrumentation causing overhead)
- [ ] **APM:** PII sanitization enabled (SQL parameters, user data not captured)
- [ ] **Connection pool:** MaxPoolSize configured (not using default unlimited)
- [ ] **Connection pool:** All DbContext uses `using` or `await using` (no leaks)
- [ ] **Load testing:** Tested at target concurrency (not just 1-10 users)
- [ ] **Load testing:** Tested for extended duration (30+ minutes to catch memory leaks)
- [ ] **SignalR:** Using groups, not broadcasting to all clients (verify scalability)
- [ ] **Indexes:** Foreign keys indexed (verify with `EXPLAIN ANALYZE` in PostgreSQL)
- [ ] **Monitoring:** Connection pool metrics tracked (know when approaching limits)
- [ ] **Monitoring:** Query duration tracked (detect regressions)

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| **Thread pool exhaustion** | LOW | 1. Restart application (immediate relief)<br>2. Audit code for `.Result`/`.Wait()` calls<br>3. Deploy async fixes<br>4. Monitor thread pool metrics |
| **Cache stampede** | MEDIUM | 1. Temporarily increase cache TTL (reduces stampede frequency)<br>2. Add request coalescing/locking<br>3. Consider HybridCache migration<br>4. Add cache warming for critical keys |
| **N+1 queries** | LOW-MEDIUM | 1. Add missing indexes (immediate relief)<br>2. Add `.Include()` or projections<br>3. Consider read-through cache<br>4. Add query count monitoring |
| **Connection pool exhaustion** | LOW | 1. Increase MaxPoolSize temporarily<br>2. Restart to clear leaked connections<br>3. Audit for missing `using` statements<br>4. Fix background task context usage |
| **Stale cache data** | LOW | 1. Manual cache flush (immediate fix)<br>2. Reduce TTL temporarily<br>3. Implement cache invalidation<br>4. Add cache versioning |
| **APM overhead** | LOW | 1. Reduce sampling rate in config<br>2. Disable expensive instrumentation<br>3. Restart with new config<br>4. Consider agent upgrade |
| **Over-optimization complexity** | HIGH | 1. No immediate fix - complexity is permanent<br>2. Add comprehensive tests<br>3. Document optimization rationale<br>4. Plan gradual simplification |
| **Memory leak** | MEDIUM | 1. Restart application<br>2. Collect memory dump for analysis<br>3. Use dotnet-gcdump to find leak source<br>4. Fix object retention<br>5. Deploy fix and monitor |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| **Async-over-sync conversion issues** | Phase 1: Foundation | Audit all controllers/services, no blocking calls, load test shows no thread pool starvation |
| **Cache stampede** | Phase 2: Caching Implementation | Load test with 100 concurrent requests on cache miss, verify single DB query |
| **N+1 queries persist** | Phase 3: Database Optimization | Enable query logging, integration tests count queries, all queries ≤3 per request |
| **Cache invalidation bugs** | Phase 2: Caching Implementation | Integration tests verify cache cleared after writes, E2E tests check data freshness |
| **Over-optimization** | Phase 1: Foundation | Performance budgets set, benchmarks required for optimizations, code review checks complexity |
| **APM overhead** | Phase 5: Monitoring Implementation | Load test with APM shows <10% overhead, sampling configured, metrics tracked |
| **Connection pool exhaustion** | Phase 1: Foundation | All DbContext uses `using`, background tasks use IDbContextFactory, pool metrics monitored |

---

## Sources

**Caching:**
- [Caching in ASP.NET Core: Improving Application Performance](https://www.milanjovanovic.tech/blog/caching-in-aspnetcore-improving-application-performance)
- [Overview of caching in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview?view=aspnetcore-10.0)
- [Performance Tuning in ASP.NET Core: Best Practices for 2026 | Syncfusion Blogs](https://www.syncfusion.com/blogs/post/performance-tuning-in-aspnetcore-2026)
- [Redis Cache in 2026: Fast Paths, Fresh Data, and a Modern DX](https://thelinuxcode.com/redis-cache-in-2026-fast-paths-fresh-data-and-a-modern-dx/)
- [Redis Caching Pitfalls: Invalidation, Testing & Best Practices | Medium](https://medium.com/@QuarkAndCode/redis-caching-pitfalls-invalidation-testing-best-practices-3950a0660f1a)
- [Solving the Distributed Cache Invalidation Problem with Redis and HybridCache](https://www.milanjovanovic.tech/blog/solving-the-distributed-cache-invalidation-problem-with-redis-and-hybridcache)

**Async/Await:**
- [Don't Block on Async Code](https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html)
- [Understanding Async, Avoiding Deadlocks in C# | Medium](https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d)
- [.Net Async Await, the Good, the Bad and the Deadlocks](https://www.garethrepton.com/AsyncAwait/)
- [Asynchronous programming in .NET 8 — Common Pitfalls and Recommended Practices | Medium](https://admirmujkic.medium.com/asynchronous-programming-in-net-8-common-pitfalls-and-recommended-practices-593c9984d229)

**Entity Framework Core:**
- [Avoiding N+1 Queries in EF Core: Practical Patterns and Fixes | Medium](https://medium.com/@kittikawin_ball/avoiding-n-1-queries-in-ef-core-practical-patterns-and-fixes-9ef8da6a6a9f)
- [Entity Framework Core Isn't Slow; You're Just Using It Wrong](https://dev.to/iamcymentho/entity-framework-core-isnt-slow-youre-just-using-it-wrong-308i)
- [Avoiding N+1 Queries in EF Core: Include() vs SplitQuery() | ByteCrafted](https://bytecrafted.dev/posts/ef-core/n-plus-one-include-vs-splitquery/)
- [How to Optimize Entity Framework Core Queries](https://oneuptime.com/blog/post/2026-01-28-optimize-entity-framework-core-queries/view)

**SignalR:**
- [SignalR Performance | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/signalr/overview/performance/signalr-performance)
- [Scaling SignalR: Scaleout strategies, limits & alternatives](https://ably.com/topic/scaling-signalr)
- [ASP.NET Core SignalR production hosting and scaling | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-9.0)

**APM/Monitoring:**
- [Top APM Tools in 2026: What Every Developer Should Know](https://dev.to/olivia_madison_b0ad7090ad/top-apm-tools-in-2026-what-every-developer-and-engineering-team-should-know-1dg0)
- [APM .NET Agent Performance Overhead | Site24x7](https://www.site24x7.com/help/apm/dotnet-agent/agent-performance-report.html)

**Connection Pooling:**
- [Scalable and Performant ASP.NET Core Web APIs: Database Connections](https://carlrippon.com/scalable-and-performant-asp-net-core-web-apis-database-connections/)
- [How to prevent connection pool problems between ASP.NET and SQL Server?](https://www.mytecbits.com/microsoft/dot-net/prevent-connection-pool-problems)
- [Troubleshoot Connection Pooling Issue Between .Net & SQL - Site24x7](https://www.site24x7.com/learn/troubleshoot-connection-pooling-issue.html)

**Premature Optimization:**
- [Why Premature Optimization Is the Root of All Evil - Stackify](https://stackify.com/premature-optimization-evil/)
- [Premature Optimization: Stop Over-Engineering](https://www.qt.io/quality-assurance/blog/premature-optimization)
- [Why Premature Optimization is the Root of All Evil? - GeeksforGeeks](https://www.geeksforgeeks.org/software-engineering/premature-optimization/)

**Load Testing:**
- [ASP.NET Core load/stress testing | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/test/load-tests?view=aspnetcore-10.0)
- [Scalable and Performant ASP.NET Core Web APIs: Load Testing](https://carlrippon.com/scalable-and-performant-asp-net-core-web-apis-load-testing/)

**Microsoft Official Documentation:**
- [ASP.NET Core Best Practices | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0)
- [Overview of caching in ASP.NET Core | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview?view=aspnetcore-10.0)

---

*Pitfalls research for: CrowdQR Performance Optimization Milestone*
*Researched: 2026-02-05*
*Project context: Adding caching, async optimization, query optimization, and monitoring to existing ASP.NET Core 8.0 application*
