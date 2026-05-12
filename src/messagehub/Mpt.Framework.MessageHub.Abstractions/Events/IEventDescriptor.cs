namespace Mpt.Framework.MessageHub;

/// <summary>
/// Per-event customisations applied by <see cref="PlatformEvent.Customize(Action{IEventDescriptor})"/>.
/// Lets callers override the routing event key, entity display labels, summary/description,
/// attach extra objects, set hints, or suppress publication entirely.
/// </summary>
public interface IEventDescriptor
{
    /// <summary>Override for <see cref="EventMessageRouting.Event"/>.</summary>
    string? EventKey { get; set; }

    /// <summary>Override for the resolved entity display name.</summary>
    string? EntityName { get; set; }

    /// <summary>Override for the main entity object key.</summary>
    string? EntityKey { get; set; }

    /// <summary>Override for <see cref="EventMessageInfo.Summary"/>.</summary>
    string? Summary { get; set; }

    /// <summary>Override for <see cref="EventMessageInfo.Description"/>.</summary>
    string? Description { get; set; }

    /// <summary>Extra <see cref="EventMessageObject"/> instances to append.</summary>
    List<EventMessageObject>? AdditionalObjects { get; set; }

    /// <summary>Override for <see cref="EventMessage.Hints"/>.</summary>
    EventHints? Hints { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the event is dropped silently — neither the producer
    /// nor the emitter will publish it.
    /// </summary>
    bool IsSuppressed { get; set; }
}

internal class EventDescriptor : IEventDescriptor
{
    public string? EventKey { get; set; }
    public string? EntityName { get; set; }
    public string? EntityKey { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public List<EventMessageObject>? AdditionalObjects { get; set; }
    public EventHints? Hints { get; set; }
    public bool IsSuppressed { get; set; }
}
