using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

public interface ISyncPlatformEventProducer
{
    IAsyncEnumerable<PlatformEvent> ProduceSyncEventsAsync(object entity, CancellationToken cancellationToken);
}

public interface ISyncPlatformEventProducer<TEntity> : ISyncPlatformEventProducer
{
}
