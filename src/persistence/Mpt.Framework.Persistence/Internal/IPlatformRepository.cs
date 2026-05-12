namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Internal contract <see cref="UnitOfWork"/> uses to coordinate the multi-phase save
/// flow across every registered repository. Exposed as <see langword="internal"/> in
/// the engine assembly + <c>InternalsVisibleTo</c> for the EFCore add-on and tests.
/// </summary>
internal interface IPlatformRepository : IRepository
{
    void ResetStorage();

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task OnSaveChangesInitiatedAsync(DateTime timestamp, CancellationToken cancellationToken);

    Task OnBeforeSaveChangesAsync(DateTime timestamp, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the persistence flush has committed. Repositories invoke their
    /// <see cref="IEntityEventProducer{TEntity}"/> here to register events with the
    /// shared <see cref="MessageHub.IPlatformEventEmitter"/>; the unit of work flushes
    /// the emitter once every repository has produced.
    /// </summary>
    Task OnAfterSaveChangesAsync(CancellationToken cancellationToken);
}
