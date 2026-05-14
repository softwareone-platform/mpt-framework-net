using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation;

[ExcludeFromCodeCoverage(Justification = "POCO with a single Guid init-property.")]
public class OperationMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
