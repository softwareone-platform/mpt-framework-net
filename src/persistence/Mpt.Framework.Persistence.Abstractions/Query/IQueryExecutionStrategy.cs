namespace Mpt.Framework.Persistence;

/// <summary>
/// Pluggable async execution layer for the query service. The in-memory strategy
/// (shipped in the engine package) executes synchronously over LINQ-to-objects; the
/// EF Core add-on substitutes EF Core's async query operators so that real database
/// I/O happens asynchronously.
/// </summary>
public interface IQueryExecutionStrategy
{
    /// <summary>Executes the query and returns the first result (or default).</summary>
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);

    /// <summary>Executes the query and materialises every row.</summary>
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);

    /// <summary>Executes the query and counts the rows.</summary>
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken);
}
