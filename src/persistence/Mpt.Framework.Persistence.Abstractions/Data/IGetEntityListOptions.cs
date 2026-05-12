using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Knobs available on a list read — paging window plus ordering.
/// </summary>
public interface IGetEntityListOptions<TEntity> : IGetEntityOptions
{
    /// <summary>Maximum number of entities to return.</summary>
    int Limit { get; set; }

    /// <summary>Number of entities to skip before the page starts.</summary>
    int Offset { get; set; }

    /// <summary>Adds a primary ascending order clause on the supplied property.</summary>
    IListOrderOptions<TEntity> OrderBy(Expression<Func<TEntity, object>> property);

    /// <summary>Adds a primary descending order clause on the supplied property.</summary>
    IListOrderOptions<TEntity> OrderByDescending(Expression<Func<TEntity, object>> property);
}
