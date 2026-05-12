using Mpt.Framework.Mapping;
using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Internal;
using Mpt.Rql;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Abstract base for per-entity repositories. Subclass for each (TDbEntity, TEntity)
/// pair and provide the persistence-specific overrides (<see cref="StartAddEntityAsync"/>,
/// <see cref="CompleteAddEntityAsync"/>, <see cref="UpdateEntityAsync"/>,
/// <see cref="DeleteEntityAsync"/>, <see cref="SaveChangesAsync"/>, <see cref="ResetStorage"/>,
/// <see cref="CountAsync(Expression{Func{TEntity, bool}}, Action{IGetEntityListOptions{TEntity}}, CancellationToken)"/>).
/// The EFCore add-on provides the SQL-backed implementation.
/// </summary>
public abstract class Repository<TEntity>(
    IQueryService<TEntity> queryService,
    IEntityEventProducer<TEntity> eventProducer,
    IEntityLifecycleHooks<TEntity> lifecycleHooks,
    IInMemoryEntityMapper inMemoryEntityMapper) : IRepository<TEntity>, IPlatformRepository
    where TEntity : class, IPlatformEntity, new()
{
    private HashSet<TEntity>? _creating;
    private HashSet<TEntity>? _updating;
    private Dictionary<TEntity, TEntity>? _updatingOriginals;
    private HashSet<TEntity>? _deleting;

    /// <inheritdoc />
    public async Task<TEntity?> GetForUpdateAsync(string id, CancellationToken cancellationToken)
    {
        var item = await queryService.GetAsync(id, cancellationToken);
        if (item != null)
            await TrackForUpdateAsync(item);
        return item;
    }

    /// <inheritdoc />
    public Task<TEntity?> GetAsync(string id, Action<IGetEntityOptions> configure, CancellationToken cancellationToken)
    {
        var options = ConfigureOptions<GetEntityOptions>(configure);
        return queryService.GetAsync(id, options.Request, static q => q, cancellationToken, options.ApplyConfiguration);
    }

    /// <inheritdoc />
    public Task<TResult?> GetShapedAsync<TResult>(string id, Expression<Func<TEntity, TResult>> shaper, Action<IGetEntityOptions> configure, CancellationToken cancellationToken)
    {
        var options = ConfigureOptions<GetEntityOptions>(configure);
        return queryService.GetShapedAsync(id, options.Request, shaper, cancellationToken, options.ApplyConfiguration);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetReferenceAsync(string id, CancellationToken cancellationToken)
        => queryService.GetAsync(id, cancellationToken, settings =>
        {
            settings.Select.Implicit = settings.Select.Explicit = RqlSelectModes.Core;
        });

    /// <inheritdoc />
    public async Task<List<TEntity>> ListForUpdateAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken)
    {
        var dataPage = await GetDataPage(filter, cfg =>
        {
            cfg.Configure(RqlDefaults.SetSingleItemDefaults);
            configure(cfg);
        }, cancellationToken);

        foreach (var item in dataPage.Data)
            await TrackForUpdateAsync(item);

        return dataPage.Data;
    }

    /// <inheritdoc />
    public async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken)
    {
        var dataPage = await GetDataPage(filter, configure, cancellationToken);
        return dataPage.Data;
    }

    /// <summary>Resolves the page request, applies the predicate, runs through the query service.</summary>
    protected async Task<DataPage<TEntity>> GetDataPage(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken, bool countAllRecords = false)
    {
        var options = ConfigureOptions<GetEntityListOptions<TEntity>>(configure);
        var pageRequest = new DataPageRequest(options.Request, options.Limit, options.Offset, countAllRecords);
        return await queryService.GetPageAsync(pageRequest, q => TryApplyOrderBy(q.Where(filter), options.Order), cancellationToken, options.ApplyConfiguration);
    }

    private static IQueryable<TEntity> TryApplyOrderBy(IQueryable<TEntity> query, ListOrderOptions<TEntity>? options)
    {
        if (options == null)
            return query;

        IOrderedQueryable<TEntity>? result = null;

        foreach (var (property, direction) in options.Enumerate())
        {
            if (result == null)
                result = direction > 0 ? query.OrderBy(property) : query.OrderByDescending(property);
            else
                result = direction > 0 ? result.ThenBy(property) : result.ThenByDescending(property);
        }

        return result ?? query;
    }

    private static TOptions ConfigureOptions<TOptions>(Action<TOptions> configure)
        where TOptions : GetEntityOptions, new()
    {
        var options = new TOptions();
        configure(options);
        return options;
    }

    /// <inheritdoc />
    public void Add(TEntity item)
    {
        _creating ??= [];
        _creating.Add(item);
    }

    private async Task TrackForUpdateAsync(TEntity item)
    {
        _updating ??= [];
        _updating.Add(item);

        _updatingOriginals ??= [];
        var original = new TEntity();
        await inMemoryEntityMapper.MapAsync(item, original);
        _updatingOriginals.Add(item, original);
    }

    /// <inheritdoc />
    public abstract Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken);

    /// <inheritdoc />
    public void Delete(TEntity item)
    {
        _deleting ??= [];
        _deleting.Add(item);
    }

    /// <summary>Persistence-specific hook invoked once per Add'd entity before <c>OnCreatingAsync</c> fires (typically wires the entity into the change tracker so id-generation completes before lifecycle hooks see it).</summary>
    protected abstract Task StartAddEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Persistence-specific hook invoked once per Add'd entity after <c>OnCreatingAsync</c> fires.</summary>
    protected abstract Task CompleteAddEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Persistence-specific update hook. Returns <see langword="false"/> when nothing actually changed so the engine can suppress the produced "Updated" event.</summary>
    protected abstract Task<bool> UpdateEntityAsync(IEntityUpdatingContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Persistence-specific delete hook.</summary>
    protected abstract Task DeleteEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Discards any persistence-side tracker state. Called when the unit of work resets.</summary>
    public abstract void ResetStorage();

    /// <summary>Flushes pending changes to the persistence store. EF Core implementations call <c>DbContext.SaveChangesAsync</c>.</summary>
    public abstract Task SaveChangesAsync(CancellationToken cancellationToken);

    async Task IPlatformRepository.OnSaveChangesInitiatedAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        if (_creating == null)
            return;

        foreach (var item in _creating)
        {
            var context = new EntityActionContext<TEntity>(item, timestamp);
            await StartAddEntityAsync(context, cancellationToken);
        }
    }

    async Task IPlatformRepository.OnBeforeSaveChangesAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        await ProcessCreatingAsync(timestamp, cancellationToken);
        await ProcessUpdatingAsync(timestamp, cancellationToken);
        await ProcessDeletingAsync(timestamp, cancellationToken);
    }

    private async Task ProcessCreatingAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        if (_creating == null)
            return;

        foreach (var item in _creating)
        {
            var context = new EntityActionContext<TEntity>(item, timestamp);
            await lifecycleHooks.OnCreatingAsync(context, cancellationToken);
            await CompleteAddEntityAsync(context, cancellationToken);
        }
    }

    private async Task ProcessUpdatingAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        if (_updating == null)
            return;

        foreach (var item in _updating.ToList())
        {
            var original = _updatingOriginals?.TryGetValue(item, out var found) == true ? found : item;
            var context = new EntityUpdatingContext<TEntity>(original, item, timestamp);
            await lifecycleHooks.OnUpdatingAsync(context, cancellationToken);
            var hasChanges = await UpdateEntityAsync(context, cancellationToken);

            if (!hasChanges)
                _updating.Remove(item);
        }
    }

    private async Task ProcessDeletingAsync(DateTime timestamp, CancellationToken cancellationToken)
    {
        if (_deleting == null)
            return;

        foreach (var item in _deleting)
        {
            var context = new EntityActionContext<TEntity>(item, timestamp);
            await lifecycleHooks.OnDeletingAsync(context, cancellationToken);
            await DeleteEntityAsync(context, cancellationToken);
        }
    }

    async Task IPlatformRepository.OnAfterSaveChangesAsync(CancellationToken cancellationToken)
    {
        await ProduceCreatedAsync(_creating, cancellationToken);
        await ProduceUpdatedAsync(_updating, cancellationToken);
        await ProduceDeletedAsync(_deleting, cancellationToken);

        _creating = null;
        _updating = null;
        _updatingOriginals = null;
        _deleting = null;
    }

    private async Task ProduceCreatedAsync(HashSet<TEntity>? items, CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0) return;
        if (!eventProducer.ShouldProduceOn(EntityAction.Create)) return;

        foreach (var item in items)
        {
            await eventProducer.ProduceCreatedEvents(item, cancellationToken);
            await eventProducer.ProduceCustomEvents(item, null, cancellationToken);
        }
    }

    private async Task ProduceUpdatedAsync(HashSet<TEntity>? items, CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0) return;
        if (!eventProducer.ShouldProduceOn(EntityAction.Update)) return;

        foreach (var item in items)
        {
            var original = _updatingOriginals?.GetValueOrDefault(item);
            await eventProducer.ProduceUpdatedEvents(item, original, cancellationToken);
            await eventProducer.ProduceCustomEvents(item, original, cancellationToken);
        }
    }

    private async Task ProduceDeletedAsync(HashSet<TEntity>? items, CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0) return;
        if (!eventProducer.ShouldProduceOn(EntityAction.Delete)) return;

        foreach (var item in items)
        {
            await eventProducer.ProduceDeletedEvents(item, cancellationToken);
            await eventProducer.ProduceCustomEvents(item, null, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<object?> GetReferenceAsObjectAsync(string id, CancellationToken cancellationToken)
        => GetReferenceAsync(id, cancellationToken).ContinueWith(t => (object?)t.Result, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

    /// <inheritdoc />
    public void ResetChanges()
    {
        _creating = null;
        _updating = null;
        _deleting = null;
        _updatingOriginals = null;
    }
}
