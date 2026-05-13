namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic marker for lifecycle-hook implementations — exists so DI can locate
/// them by interface during assembly scanning.
/// </summary>
public interface IEntityLifecycleHooks;

/// <summary>
/// Per-entity hooks invoked by <see cref="IRepository{TEntity}"/> immediately before
/// the underlying persistence layer commits each pending entity. Override to apply
/// invariants, derived values, or domain rules just-in-time before the write.
/// </summary>
public interface IEntityLifecycleHooks<in TEntity> : IEntityLifecycleHooks where TEntity : IPlatformEntity
{
    /// <summary>Invoked once for each entity being created, before the persistence-side insert.</summary>
    Task OnCreatingAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Invoked once for each entity being updated, with both original and current state.</summary>
    Task OnUpdatingAsync(IEntityUpdatingContext<TEntity> context, CancellationToken cancellationToken);

    /// <summary>Invoked once for each entity being deleted, before the persistence-side delete.</summary>
    Task OnDeletingAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken);
}
