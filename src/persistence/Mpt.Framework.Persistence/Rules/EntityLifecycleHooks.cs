namespace Mpt.Framework.Persistence;

/// <summary>
/// No-op default implementation of <see cref="IEntityLifecycleHooks{TEntity}"/>. Derive
/// and override the specific lifecycle methods you need.
/// </summary>
public class EntityLifecycleHooks<TEntity> : IEntityLifecycleHooks<TEntity> where TEntity : IPlatformEntity
{
    /// <inheritdoc />
    public virtual Task OnCreatingAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnUpdatingAsync(IEntityUpdatingContext<TEntity> context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnDeletingAsync(IEntityActionContext<TEntity> context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
