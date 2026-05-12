using Microsoft.EntityFrameworkCore;

namespace Mpt.Framework.Persistence.EFCore;

/// <summary>
/// EF Core <see cref="IQueryExecutionStrategy"/> — delegates async query operations to
/// the EF Core async extensions so they actually run asynchronously against the
/// database (the in-memory strategy in the engine package is synchronous).
/// </summary>
public class EfCoreQueryExecutionStrategy : IQueryExecutionStrategy
{
    /// <summary>Process-wide singleton instance.</summary>
    public static EfCoreQueryExecutionStrategy Instance { get; } = new();

    /// <inheritdoc />
    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => EntityFrameworkQueryableExtensions.CountAsync(query, cancellationToken);

    /// <inheritdoc />
    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query, cancellationToken);

    /// <inheritdoc />
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);
}
