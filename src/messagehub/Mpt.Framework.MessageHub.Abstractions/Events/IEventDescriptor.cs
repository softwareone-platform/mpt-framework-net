namespace Mpt.Framework.MessageHub;

/// <summary>
/// Per-event customisations applied by <see cref="PlatformEvent.Customize(Action{IEventDescriptor})"/>.
/// </summary>
public interface IEventDescriptor
{
    string? EventKey { get; set; }

    string? EntityName { get; set; }

    string? EntityKey { get; set; }

    string? Summary { get; set; }

    string? Description { get; set; }

    List<EventMessageObject>? AdditionalObjects { get; set; }

    EventHints? Hints { get; set; }

    /// <summary>When <see langword="true"/>, the event is dropped silently — neither the producer nor the emitter will publish it.</summary>
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
