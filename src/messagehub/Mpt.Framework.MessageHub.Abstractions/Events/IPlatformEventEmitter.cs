namespace Mpt.Framework.MessageHub;

/// <summary>
/// Scoped batching seam that collects <see cref="IPlatformEvent"/> instances and flushes
/// them as <see cref="EventMessage"/> publications when <see cref="EmitAsync"/> runs.
/// </summary>
public interface IPlatformEventEmitter
{
    void Register(IPlatformEvent entityEvent);

    void Register(IEnumerable<IPlatformEvent> entityEvents);

    Task EmitAsync(CancellationToken cancellationToken);

    Task EmitSingleAsync(IPlatformEvent entityEvent, CancellationToken cancellationToken);

    void Reset();
}
