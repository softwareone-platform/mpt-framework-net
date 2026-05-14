using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Configuration;

[ExcludeFromCodeCoverage(Justification = "POCO settings carrier with auto-properties only.")]
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
