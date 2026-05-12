namespace Mpt.Framework.Persistence;

/// <summary>
/// In-memory query execution strategy. Executes synchronously against LINQ-to-objects
/// and wraps the result in a completed task. The shipping default; the EFCore add-on
/// substitutes its own strategy that delegates to EF Core's async query operators.
/// </summary>
public class InMemoryQueryExecutionStrategy : IQueryExecutionStrategy
{
    /// <summary>Process-wide singleton instance.</summary>
    public static InMemoryQueryExecutionStrategy Instance { get; } = new();

    /// <inheritdoc />
    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => Task.FromResult(query.Count());

    /// <inheritdoc />
    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => Task.FromResult(query.FirstOrDefault());

    /// <inheritdoc />
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        => Task.FromResult(query.ToList());
}
