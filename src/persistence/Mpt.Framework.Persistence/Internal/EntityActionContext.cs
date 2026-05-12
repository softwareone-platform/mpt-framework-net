namespace Mpt.Framework.Persistence.Internal;

internal class EntityActionContext<TEntity>(TEntity entity, DateTime timestamp) : IEntityActionContext<TEntity>
    where TEntity : class, IPlatformEntity
{
    public TEntity Entity { get; } = entity;

    public DateTime Timestamp { get; } = timestamp;
}

internal class EntityUpdatingContext<TEntity>(TEntity original, TEntity entity, DateTime timestamp)
    : EntityActionContext<TEntity>(entity, timestamp), IEntityUpdatingContext<TEntity>
    where TEntity : class, IPlatformEntity
{
    public TEntity Original { get; } = original;
}
