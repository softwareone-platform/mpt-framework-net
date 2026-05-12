namespace Mpt.Framework.MessageHub;

/// <summary>
/// Scoped batching seam that collects <see cref="IPlatformEvent"/> instances during a unit
/// of work and flushes them as <see cref="EventMessage"/> publications via the registered
/// publish-mode pipeline. Inject from application services to produce events; call
/// <see cref="EmitAsync"/> at the end of the work to ship them.
/// </summary>
public interface IPlatformEventEmitter
{
    /// <summary>Queues a single event for emission.</summary>
    void Register(IPlatformEvent entityEvent);

    /// <summary>Queues a batch of events for emission.</summary>
    void Register(IEnumerable<IPlatformEvent> entityEvents);

    /// <summary>Drains the queue and publishes each event concurrently.</summary>
    Task EmitAsync(CancellationToken cancellationToken);

    /// <summary>Publishes one event immediately, bypassing the queue.</summary>
    Task EmitSingleAsync(IPlatformEvent entityEvent, CancellationToken cancellationToken);

    /// <summary>Discards any queued events without publishing.</summary>
    void Reset();
}
