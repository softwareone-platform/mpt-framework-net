using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Default RQL settings the query service applies for the two common request shapes —
/// single-entity reads and paged lists.
/// </summary>
public static class RqlDefaults
{
    /// <summary>An empty RQL request.</summary>
    public static RqlRequest EmptyRequest { get; } = new();

    /// <summary>Defaults appropriate for fetching a single entity (eager projection of all properties).</summary>
    public static void SetSingleItemDefaults(IRqlSettings settings)
    {
        settings.Select.Explicit = RqlSelectModes.All;
        settings.Select.Implicit = RqlSelectModes.Core;
    }

    /// <summary>Defaults appropriate for paged list queries (project primitives + references only).</summary>
    public static void SetListDefaults(IRqlSettings settings)
    {
        settings.Select.Explicit = RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Implicit = RqlSelectModes.Core;
    }

    /// <summary>Defaults appropriate for the in-memory provider.</summary>
    public static void InMemoryDefaults(IRqlSettings settings)
    {
        settings.Mapping.Transparent = true;
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        settings.Filter.Navigation = NavigationStrategy.Safe;
        settings.Ordering.Navigation = NavigationStrategy.Safe;
    }
}
