namespace Mpt.Framework.Persistence;

/// <summary>
/// Aggregates per-entity <see cref="IRepository{TEntity}"/> instances and orchestrates
/// the multi-phase save flow: <c>OnSaveChangesInitiated</c> → <c>OnBeforeSaveChanges</c>
/// → persistence write → <c>OnAfterSaveChanges</c> → event publish.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Resolves the repository for the supplied entity type (cached per scope).</summary>
    IRepository<TEntity> GetRepository<TEntity>();

    /// <summary>Discards all pending changes across every repository in this unit of work.</summary>
    void ResetChanges();

    /// <summary>Commits pending changes across every repository, publishes lifecycle events.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Commits pending changes, then runs the supplied callback. Errors thrown from the
    /// callback bubble up — use the overload below if you need a failure handler.
    /// </summary>
    Task SaveChangesAsync(Func<CancellationToken, Task> afterSaveActivity, CancellationToken cancellationToken);

    /// <summary>
    /// Commits pending changes, then runs the supplied callback. If the callback throws,
    /// the failure handler is invoked instead of the exception bubbling.
    /// </summary>
    Task SaveChangesAsync(Func<CancellationToken, Task> afterSaveActivity, Func<Exception, CancellationToken, Task> afterSaveActivityFailure, CancellationToken cancellationToken);
}
