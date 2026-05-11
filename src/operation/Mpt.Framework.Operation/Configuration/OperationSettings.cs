namespace Mpt.Framework.Operation.Configuration;

public class OperationSettings
{
    public string? GlobalPrefix { get; set; }

    public OperationMode Mode { get; set; }

    public OperationTransport Transport { get; set; }

    public OperationCleanupMode Cleanup { get; set; } = OperationCleanupMode.None;

    public string? ConnectionString { get; set; }
}

public enum OperationMode
{
    Dispatch,
    ConsumeAndDispatch
}

public enum OperationTransport
{
    InMemory,
    ServiceBus
}

public enum OperationCleanupMode
{
    None,
    DeleteEmptyUnknown,
    DeleteAnyUnknown
}
