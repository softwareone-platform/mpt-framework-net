namespace Mpt.Framework.Operations.Configuration;

public class OperationSettings
{
    public string? GlobalPrefix { get; set; }

    public OperationsMode Mode { get; set; }

    public OperationsTransport Transport { get; set; }

    public OperationsCleanupMode Cleanup { get; set; } = OperationsCleanupMode.None;

    public string? ConnectionString { get; set; }
}

public enum OperationsMode
{
    Dispatch,
    ConsumeAndDispatch
}

public enum OperationsTransport
{
    InMemory,
    ServiceBus
}

public enum OperationsCleanupMode
{
    None,
    DeleteEmptyUnknown,
    DeleteAnyUnknown
}
