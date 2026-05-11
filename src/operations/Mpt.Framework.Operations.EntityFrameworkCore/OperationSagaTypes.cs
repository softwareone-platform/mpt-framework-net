namespace Mpt.Framework.Operations.EntityFrameworkCore;

internal class OperationSagaTypes(IEnumerable<(Type, string)> types)
{
    public List<(Type, string)> Types { get; } = [.. types];
}
