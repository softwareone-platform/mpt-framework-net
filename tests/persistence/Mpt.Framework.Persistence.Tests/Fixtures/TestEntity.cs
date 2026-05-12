using Mpt.Framework;
using Mpt.Framework.Persistence;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests.Fixtures;

/// <summary>The view-model the tests work against.</summary>
public class WidgetView : IPlatformEntity, IRqlGraphHolder
{
    public string Id { get; set; } = string.Empty;
    public int Revision { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public IRqlNode? RqlGraph { get; set; }
}

/// <summary>The persistence-side entity the tests work against.</summary>
public class WidgetDbEntity
{
    public string Id { get; set; } = string.Empty;
    public int Revision { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
