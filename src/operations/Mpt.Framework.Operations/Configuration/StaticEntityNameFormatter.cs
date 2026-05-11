using MassTransit;

namespace Mpt.Framework.Operations.Configuration;

internal class StaticEntityNameFormatter(string name) : IEntityNameFormatter
{
    public string FormatEntityName<T>() => name;
}
