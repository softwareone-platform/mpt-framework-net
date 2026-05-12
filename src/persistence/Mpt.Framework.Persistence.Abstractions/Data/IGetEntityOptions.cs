using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Knobs available on a single-entity read — the RQL request being made and any extra
/// per-call RQL settings.
/// </summary>
public interface IGetEntityOptions
{
    /// <summary>
    /// The RQL request describing select / filter / order / paging.
    /// </summary>
    RqlRequest Request { get; set; }

    /// <summary>
    /// Adds a configuration action that runs at query-build time against the resolved
    /// <see cref="IRqlSettings"/>. Use to tune select modes, navigation strategies, etc.
    /// </summary>
    void Configure(Action<IRqlSettings> configure);
}
