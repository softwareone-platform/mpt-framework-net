using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

public interface ISyncPlatformEventProducer
{
    IAsyncEnumerable<PlatformEvent> ProduceSyncEventsAsync(object entity, CancellationToken cancellationToken);
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "Phantom type parameter used for DI registration / resolution keying per entity type.")]
public interface ISyncPlatformEventProducer<TEntity> : ISyncPlatformEventProducer
{
}
