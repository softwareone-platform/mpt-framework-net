using MassTransit;

namespace Mpt.Framework.Operation.Configuration;

internal class StaticEntityNameFormatter(string name) : IEntityNameFormatter
{
    public string FormatEntityName<T>() => name;
}
