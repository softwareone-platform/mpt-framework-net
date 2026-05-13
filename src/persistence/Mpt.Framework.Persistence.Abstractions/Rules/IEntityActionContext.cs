namespace Mpt.Framework.Persistence;

/// <summary>
/// Context passed to <see cref="IEntityLifecycleHooks{TEntity}.OnCreatingAsync"/> and
/// <see cref="IEntityLifecycleHooks{TEntity}.OnDeletingAsync"/>. Exposes the entity being
/// acted on plus the action's timestamp.
/// </summary>
public interface IEntityActionContext<out TEntity>
{
    /// <summary>
    /// The entity being created or deleted. Mutations are persisted and trigger any
    /// downstream lifecycle hooks / event producers.
    /// </summary>
    TEntity Entity { get; }

    /// <summary>
    /// Timestamp captured at the start of the save flow.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Context passed to <see cref="IEntityLifecycleHooks{TEntity}.OnUpdatingAsync"/>. Adds
/// access to the original entity state before any in-place mutations.
/// </summary>
public interface IEntityUpdatingContext<out TEntity> : IEntityActionContext<TEntity>
{
    /// <summary>
    /// Snapshot of the entity captured at the time it was read for update. Modifying
    /// this object is generally discouraged — the engine uses it to compute deltas.
    /// </summary>
    TEntity Original { get; }
}
