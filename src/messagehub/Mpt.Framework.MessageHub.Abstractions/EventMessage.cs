namespace Mpt.Framework.MessageHub;

/// <summary>
/// Wire-level payload published over MessageHub. Carries enough metadata for downstream
/// modules to filter (via <see cref="Routing"/>) and reconstruct the event subject (via
/// <see cref="Objects"/>).
/// </summary>
public class EventMessage
{
    public string Id { get; set; } = null!;

    public int Replays { get; set; }

    public EventMessageRouting Routing { get; init; } = null!;

    public List<EventMessageObject> Objects { get; init; } = null!;

    public EventMessageInfo Info { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }

    public EventHints Hints { get; set; }

    public string? SessionId { get; set; }

    public string? PartitionKey { get; set; }

    public void Validate()
    {
        if (Objects.Count == 0)
            throw new InvalidOperationException("At least one subject must be specified");

        if (Routing == null)
            throw new InvalidOperationException("Routing must be defined");

        if ((Routing.Stream & (Routing.Stream - 1)) != 0 || Routing.Stream == StreamTypes.None)
            throw new InvalidOperationException("Routing stream cannot be combined or be None");

        if (Info == null || string.IsNullOrWhiteSpace(Info.Summary))
            throw new InvalidOperationException("Summary is required");
    }
}
