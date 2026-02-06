using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;

namespace CrowdQR.Api.Tests.Helpers;

/// <summary>
/// EF Core interceptor that counts database commands for testing query budgets.
/// </summary>
public class QueryCountingInterceptor : DbCommandInterceptor
{
    private int _queryCount;

    /// <summary>
    /// Gets the current query count since last reset.
    /// </summary>
    public int QueryCount => _queryCount;

    /// <summary>
    /// Resets the query counter to zero.
    /// </summary>
    public void Reset() => Interlocked.Exchange(ref _queryCount, 0);

    /// <summary>
    /// Intercepts synchronous reader execution to count the query.
    /// </summary>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref _queryCount);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous reader execution to count the query.
    /// </summary>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _queryCount);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Intercepts synchronous non-query execution to count the command.
    /// </summary>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Interlocked.Increment(ref _queryCount);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous non-query execution to count the command.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _queryCount);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Intercepts synchronous scalar execution to count the command.
    /// </summary>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Interlocked.Increment(ref _queryCount);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous scalar execution to count the command.
    /// </summary>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _queryCount);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
