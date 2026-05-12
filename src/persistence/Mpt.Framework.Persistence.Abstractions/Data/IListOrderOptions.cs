using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Fluent extension of a list-order clause established by <see cref="IGetEntityListOptions{TEntity}.OrderBy"/>
/// or <see cref="IGetEntityListOptions{TEntity}.OrderByDescending"/>.
/// </summary>
public interface IListOrderOptions<TEntity>
{
    /// <summary>Adds a secondary ascending order on the supplied property.</summary>
    void ThenBy(Expression<Func<TEntity, object>> property);

    /// <summary>Adds a secondary descending order on the supplied property.</summary>
    void ThenByDescending(Expression<Func<TEntity, object>> property);
}
