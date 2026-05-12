namespace Mpt.Framework.MessageHub;

/// <summary>
/// Minimal description of the principal that triggered an event. Stamped on outgoing
/// <see cref="EventMessage"/> instances by <see cref="IPlatformEventEmitter"/> when an
/// <see cref="IPlatformEventActorProducer"/> is registered.
/// </summary>
public class EventMessageActor
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? Icon { get; set; }
}
