using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic facade resolved when only the entity id is known at the call site
/// (e.g. cross-entity reference loaders).
/// </summary>
public interface IQueryService
{
    /// <summary>Asynchronously gets an entity as an object by its identifier.</summary>
    Task<object?> GetAsObjectAsync(object id, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetAsObjectAsync(id, new RqlRequest(), cancellationToken, configure);

    /// <summary>Asynchronously gets an entity as an object by id with the supplied RQL request.</summary>
    Task<object?> GetAsObjectAsync(object id, RqlRequest request, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null);
}

/// <summary>
/// Strongly-typed read surface over the persistence-backed query pipeline. The default
/// implementation lives in <c>Mpt.Framework.Persistence</c> and delegates execution to
/// an <see cref="IQueryExecutionStrategy"/> (in-memory for tests, EF Core for production).
/// </summary>
/// <typeparam name="TEntity">The view-model the query projects to.</typeparam>
public interface IQueryService<TEntity> : IQueryService
{
    /// <summary>Get a single entity by id with default RQL settings.</summary>
    Task<TEntity?> GetAsync(object id, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetAsync(id, new RqlRequest(), cancellationToken, configure);

    /// <summary>Get a single entity by id and an explicit RQL request.</summary>
    Task<TEntity?> GetAsync(object id, RqlRequest request, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetAsync(id, request, static q => q, cancellationToken, configure);

    /// <summary>Get a single entity by id, RQL request, and a custom queryable transformation.</summary>
    Task<TEntity?> GetAsync(object id, RqlRequest request, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null);

    /// <summary>Get a single entity matching the supplied RQL filter (throws if more than one match is found).</summary>
    Task<TEntity?> GetAsync(RqlRequest request, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetAsync(request, static q => q, cancellationToken, configure);

    /// <summary>Get a single entity matching the supplied RQL filter with a custom queryable transformation.</summary>
    Task<TEntity?> GetAsync(RqlRequest request, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null);

    /// <summary>Get a shaped projection (custom select expression) by entity id.</summary>
    Task<TResult?> GetShapedAsync<TResult>(string id, Expression<Func<TEntity, TResult>> shaper, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetShapedAsync(id, new RqlRequest(), shaper, cancellationToken, configure);

    /// <summary>Get a shaped projection (custom select expression) by entity id and RQL request.</summary>
    Task<TResult?> GetShapedAsync<TResult>(string id, RqlRequest request, Expression<Func<TEntity, TResult>> shaper, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null);

    /// <summary>Get a page of entities.</summary>
    Task<DataPage<TEntity>> GetPageAsync(DataPageRequest pageRequest, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => GetPageAsync(pageRequest, static q => q, cancellationToken, configure);

    /// <summary>Get a page of entities with a custom queryable transformation.</summary>
    Task<DataPage<TEntity>> GetPageAsync(DataPageRequest pageRequest, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null);

    /// <summary>Get a single entity by id, throwing <see cref="PersistenceEntityNotFoundException"/> if missing.</summary>
    async Task<TEntity> GetOrThrowAsync(object id, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => await GetAsync(id, cancellationToken, configure) ?? throw new PersistenceEntityNotFoundException(id);
}
