namespace Mpt.Framework.MessageHub;

public class EventMessageObject
{
    public required string Id { get; init; }

    public string? Name { get; init; }

    public string? Icon { get; init; }

    public string? Type { get; init; }

    public required string Key { get; init; }

    public required EventMessageObjectCategory Category { get; init; }

    public required object Data { get; init; }
}

public enum EventMessageObjectCategory
{
    CurrentEntity = 0,
    OriginalEntity = 1,
    Custom = 2,
    ActorInfo = 3,
    AdditionalEntity = 4
}
