using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.EFCore;

[ExcludeFromCodeCoverage(Justification = "DI marker that just materialises a list of (Type, name) tuples for the saga store registration.")]
internal class OperationSagaTypes(IEnumerable<(Type, string)> types)
{
    public List<(Type, string)> Types { get; } = [.. types];
}
