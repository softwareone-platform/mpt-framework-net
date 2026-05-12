using Microsoft.EntityFrameworkCore;
using Mpt.Framework.Mapping;
using Mpt.Framework.Persistence;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence.EFCore;

/// <summary>
/// EF Core flavour of <see cref="Repository{TEntity}"/>. Reads + writes the
/// <typeparamref name="TDbEntity"/> tracked by <see cref="DbContext"/>, with
/// projection between <typeparamref name="TDbEntity"/> and
/// <typeparamref name="TEntity"/> driven by <see cref="IEfCoreDynamicEntityMapper"/>.
/// </summary>
public class EfCoreRepository<TDbEntity, TEntity>(
    DbContext dbContext,
    IQueryService<TEntity> queryService,
    IEntityEventProducer<TEntity> eventProducer,
    IEntityLifecycleHooks<TEntity> lifecycleHooks,
    IEfCoreDynamicEntityMapper entityMapper,
    IInMemoryEntityMapper inMemoryEntityMapper)
    : Repository<TEntity>(queryService, eventProducer, lifecycleHooks, inMemoryEntityMapper)
    where TEntity : class, IPlatformEntity, new()
    where TDbEntity : class, new()
{
    private Dictionary<TEntity, TDbEntity>? _added;

    /// <inheritdoc />
    public override async Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, Action<IGetEntityListOptions<TEntity>> configure, CancellationToken cancellationToken)
    {
        configure += static options =>
        {
            options.Limit = 0;
            options.Offset = 0;
        };

        var pageResult = await GetDataPage(filter, configure, cancellationToken, countAllRecords: true);
        return pageResult.Total!.Value;
    }

    /// <inheritdoc />
    protected override async Task StartAddEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken)
    {
        _added ??= [];
        var item = context.Entity;
        var dbItem = new TDbEntity();
        await entityMapper.MapPrimitiveAsync(item, dbItem);
        dbContext.Add(dbItem);
        _added.Add(item, dbItem);
    }

    /// <inheritdoc />
    protected override async Task CompleteAddEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken)
    {
        if (_added == null)
            return;

        var item = context.Entity;
        if (_added.TryGetValue(item, out var dbItem))
        {
            await entityMapper.MapComplexAsync(item, dbItem);
            _added.Remove(item);
        }
    }

    /// <inheritdoc />
    protected override async Task<bool> UpdateEntityAsync(IEntityUpdatingContext<TEntity> context, CancellationToken cancellationToken)
    {
        var item = context.Entity;
        var dbItem = await dbContext.Set<TDbEntity>().FindAsync([item.Id], cancellationToken: cancellationToken)
            ?? throw new PersistenceEntityNotFoundException(item.Id);

        item.Revision++;
        var updateCount = await entityMapper.MapAsync(item, dbItem);

        // The Revision++ above counts as one change; anything > 1 is a real domain mutation.
        var hasChanges = updateCount > 1;

        if (!hasChanges)
        {
            // No real change — roll back the speculative Revision++ and detach the entity
            // from the change tracker so EF doesn't emit an UPDATE.
            item.Revision--;
            dbContext.Entry(dbItem).State = EntityState.Unchanged;
        }

        return hasChanges;
    }

    /// <inheritdoc />
    protected override async Task DeleteEntityAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken)
    {
        var item = context.Entity;
        var dbItem = await dbContext.Set<TDbEntity>().FindAsync([item.Id], cancellationToken: cancellationToken)
            ?? throw new PersistenceEntityNotFoundException(item.Id);

        dbContext.Remove(dbItem);
    }

    /// <inheritdoc />
    public override async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public override void ResetStorage() => dbContext.ChangeTracker.Clear();
}
