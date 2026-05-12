using Mpt.Rql;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Implemented by view-models that carry the RQL graph describing how they were
/// projected. The query service stamps each materialised entity with the graph so
/// downstream code can reason about which properties were selected.
/// </summary>
public interface IRqlGraphHolder
{
    /// <summary>The RQL graph attached to this projection.</summary>
    IRqlNode? RqlGraph { get; set; }
}
