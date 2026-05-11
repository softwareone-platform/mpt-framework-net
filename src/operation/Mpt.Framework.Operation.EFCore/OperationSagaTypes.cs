namespace Mpt.Framework.Operation.EFCore;

internal class OperationSagaTypes(IEnumerable<(Type, string)> types)
{
    public List<(Type, string)> Types { get; } = [.. types];
}
