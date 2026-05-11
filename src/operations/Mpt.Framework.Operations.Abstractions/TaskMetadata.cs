namespace Mpt.Framework.Operations;

public class TaskMetadata
{
    public required Guid Id { get; init; } = Guid.NewGuid();

    public required int Index { get; init; }
}
