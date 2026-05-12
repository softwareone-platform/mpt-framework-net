namespace Mpt.Framework.MessageHub;

/// <summary>
/// Supplies the principal that <see cref="IPlatformEventEmitter"/> stamps on outgoing
/// events as an <see cref="EventMessageObjectCategory.ActorInfo"/> object. Optional —
/// no actor object is added when no producer is registered or the call returns <see langword="null"/>.
/// </summary>
public interface IPlatformEventActorProducer
{
    Task<EventMessageActor?> GetActor(CancellationToken token = default);
}
