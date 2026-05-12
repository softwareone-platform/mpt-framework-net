namespace Mpt.Framework.MessageHub;

/// <summary>
/// Supplies the principal that should be stamped on outgoing events as an actor
/// <see cref="EventMessageObject"/> (<see cref="EventMessageObjectCategory.ActorInfo"/>).
/// Registration is optional — when no producer is registered or the call returns
/// <see langword="null"/>, no actor object is added.
/// </summary>
public interface IPlatformEventActorProducer
{
    /// <summary>Returns the current actor, or <see langword="null"/> if none can be resolved.</summary>
    Task<EventMessageActor?> GetActor(CancellationToken token = default);
}
