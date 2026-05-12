using Mpt.Rql;

namespace Mpt.Framework.Persistence;

/// <summary>
/// A page of query results, returned by the <c>GetPageAsync</c> family on
/// <see cref="IQueryService{TEntity}"/>.
/// </summary>
/// <typeparam name="T">The shape of each row.</typeparam>
public sealed class DataPage<T>
{
    /// <summary>
    /// Total number of matching rows across all pages, or <see langword="null"/> if the
    /// caller did not request a count.
    /// </summary>
    public int? Total { get; set; }

    /// <summary>
    /// The materialised rows of this page.
    /// </summary>
    public List<T> Data { get; set; } = null!;

    /// <summary>
    /// The RQL graph describing the projection that produced this page.
    /// </summary>
    public IRqlNode RqlGraph { get; set; } = null!;
}
