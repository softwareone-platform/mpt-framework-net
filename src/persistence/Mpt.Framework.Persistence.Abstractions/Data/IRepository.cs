using Mpt.Rql;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic facade resolved when only the entity id is known at the call site.
/// </summary>
public interface IRepository
{
    /// <summary>Loads a reference projection (id + minimal metadata) by id.</summary>
    Task<object?> GetReferenceAsObjectAsync(string id, CancellationToken cancellationToken);

    /// <summary>Discards all pending Add / Delete / GetForUpdate state.</summary>
    void ResetChanges();
}

/// <summary>
/// Per-entity-type write surface backed by an <see cref="IUnitOfWork"/>. Pending
/// changes (<see cref="Add"/>, <see cref="Delete"/>, <c>GetForUpdate</c>) are flushed
/// when the unit of work commits.
/// </summary>
public interface IRepository<TEntity> : IRepository
{
    /// <summary>Loads an entity by id and marks it as a candidate for update on save.</summary>
    Task<TEntity?> GetForUpdateAsync(string id, CancellationToken cancellationToken);

    /// <summary>Loads an entity by id, throwing <see cref="PersistenceEntityNotFoundException"/> if missing, and marks it as a candidate for update.</summary>
    async Task<TEntity> GetForUpdateOrThrowAsync(string id, CancellationToken cancellationToken)
        => await GetForUpdateAsync(id, cancellationToken) ?? throw new PersistenceEntityNotFoundException(id);

    /// <summary>Loads an entity by id (read-only) with default options.</summary>
    Task<TEntity?> GetAsync(string id, CancellationToken cancellationToken)
        => GetAsync(id, static _ => { }, cancellationToken);

    /// <summary>Loads an entity by id (read-only) with custom options.</summary>
    Task<TEntity?> GetAsync(string id, Action<IGetEntityOptions> configure, CancellationToken cancellationToken);

    /// <summary>Loads a shaped projection of an entity by id.</summary>
    Task<TResult?> GetShapedAsync<TResult>(string id, Expression<Func<TEntity, TResult>> shaper, CancellationToken cancellationToken)
        => GetShapedAsync(id, shaper, static _ => { }, cancellationToken);

    /// <summary>Loads a shaped projection of an entity by id with custom options.</summary>
    Task<TResult?> GetShapedAsync<TResult>(string id, Expression<Func<TEntity, TResult>> shaper, Action<IGetEntityOptions> configure, CancellationToken cancellationToken);

    /// <summary>Loads an entity by id (read-only), throwing if missing.</summary>
    Task<TEntity> GetOrThrowAsync(string id, CancellationToken cancellationToken)
        => GetOrThrowAsync(id, static _ => { }, cancellationToken);

    /// <summary>Loads an entity by id (read-only) with custom options, throwing if missing.</summary>
    async Task<TEntity> GetOrThrowAsync(string id, Action<IGetEntityOptions> configure, CancellationToken cancellationToken)
        => await GetAsync(id, configure, cancellationToken) ?? throw new PersistenceEntityNotFoundException(id);

    /// <summary>Loads a reference projection (id + minimal metadata) by id.</summary>
    Task<TEntity?> GetReferenceAsync(string id, CancellationToken cancellationToken);

    /// <inheritdoc />
    async Task<object?> IRepository.GetReferenceAsObjectAsync(string id, CancellationToken cancellationToken)
        => await GetReferenceAsync(id, cancellationToken);

    /// <summary>Lists entities matching the filter and marks them as candidates for update.</summary>
    Task<List<TEntity>> ListForUpdateAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
        => ListForUpdateAsync(filter, static _ => { }, cancellationToken);

    /// <summary>Lists entities matching the filter with custom list options and marks them as candidates for update.</summary>
    Task<List<TEntity>> ListForUpdateAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken);

    /// <summary>Lists entities matching the filter (read-only) with default options.</summary>
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
        => ListAsync(filter, static _ => { }, cancellationToken);

    /// <summary>Lists entities matching the filter (read-only) with custom options.</summary>
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken);

    /// <summary>Counts the entities matching an RQL request.</summary>
    Task<int> CountAsync(RqlRequest request, CancellationToken cancellationToken)
        => CountAsync(static _ => true, cfg => cfg.Request = request, cancellationToken);

    /// <summary>Counts the entities matching the filter.</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken)
        => CountAsync(filter, static _ => { }, cancellationToken);

    /// <summary>Counts the entities matching the filter with custom options.</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken);

    /// <summary>Marks an entity to be inserted on the next save.</summary>
    void Add(TEntity item);

    /// <summary>Marks an entity to be deleted on the next save.</summary>
    void Delete(TEntity item);
}
