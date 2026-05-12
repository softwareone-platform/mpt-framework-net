namespace Mpt.Framework.Persistence;

/// <summary>
/// Base class for custom filter providers. Override <see cref="ApplyFilter"/> to handle
/// each named filter in your domain.
/// </summary>
public abstract class FilterProvider<TDbModel> : IFilterProvider<TDbModel> where TDbModel : class
{
    /// <inheritdoc />
    public IQueryable<TDbModel> Apply(CustomFilters filtersParams, IQueryable<TDbModel> query)
    {
        foreach (var filter in filtersParams.Get())
        {
            query = ApplyFilter(filter, query);
        }

        return query;
    }

    /// <summary>
    /// Apply a single named filter to the query. Default-impl throws — override per filter name.
    /// </summary>
    protected virtual IQueryable<TDbModel> ApplyFilter(CustomFilter filter, IQueryable<TDbModel> query)
        => throw new InvalidOperationException($"Filter '{filter.Key}' is not supported");
}

/// <summary>
/// No-op default filter provider — accepts no custom filters. Register this for entities
/// that don't have any custom filters defined.
/// </summary>
public class DefaultFilterProvider<TEntity> : FilterProvider<TEntity> where TEntity : class;
