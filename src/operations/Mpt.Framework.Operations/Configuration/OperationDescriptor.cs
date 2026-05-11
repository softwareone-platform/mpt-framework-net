namespace Mpt.Framework.Operations.Configuration;

internal class OperationDescriptor
{
    public required string Name { get; init; }

    public required string ModuleCode { get; init; }

    public required Type ImplementationType { get; init; }

    public required Type SagaType { get; init; }

    public required Type OperationType { get; init; }

    public required Type TaskType { get; init; }

    public ProcessingOptions Processing { get; } = new();

    public required string? GlobalPrefix { get; init; }

    public string TopicName => $"{GlobalPrefix ?? string.Empty}{ModuleCode}.operations";

    public string GetQueueName(MessageGroup group) => $"{TopicName}.{GetTargetName(group)}";

    public string GetTargetName(MessageGroup group) => $"{Name}.{group.ToString().ToLowerInvariant()}";
}

internal enum MessageGroup
{
    Main,
    Tasks,
    Events
}
