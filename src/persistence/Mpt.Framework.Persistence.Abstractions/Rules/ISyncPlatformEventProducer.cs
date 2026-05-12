using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Forward-compatibility marker for a sync-stream event producer. No concrete
/// implementation ships in this package; intended for consumers who roll their own
/// sync pipeline distinct from the regular event stream.
/// </summary>
public interface ISyncPlatformEventProducer
{
    /// <summary>Yields <see cref="PlatformEvent"/> instances destined for the sync stream.</summary>
    IAsyncEnumerable<PlatformEvent> ProduceSyncEventsAsync(object entity, CancellationToken cancellationToken);
}

/// <summary>Generic shape of <see cref="ISyncPlatformEventProducer"/> bound to an entity type.</summary>
public interface ISyncPlatformEventProducer<TEntity> : ISyncPlatformEventProducer
{
}
