using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation;

[ExcludeFromCodeCoverage(Justification = "POCO with two init-only properties.")]
public class TaskMetadata
{
    public required Guid Id { get; init; } = Guid.NewGuid();

    public required int Index { get; init; }
}
