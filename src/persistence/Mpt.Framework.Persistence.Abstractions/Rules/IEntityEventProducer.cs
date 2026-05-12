using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

public interface IEntityEventProducer;

/// <summary>
/// Per-entity event producer. Invoked by <see cref="IRepository{TEntity}"/> in the
/// after-save phase to register lifecycle events with <see cref="IPlatformEventEmitter"/>;
/// the unit of work flushes the emitter once every repository has produced.
/// </summary>
public interface IEntityEventProducer<TEntity> : IEntityEventProducer
    where TEntity : class, IPlatformEntity, new()
{
    bool ShouldProduceOn(EntityAction action);

    async Task ProduceCreatedEvents(TEntity entity, CancellationToken cancellationToken)
        => await ProduceCreatedEvents(entity, _ => { }, cancellationToken);

    Task ProduceCreatedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    async Task ProduceUpdatedEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
        => await ProduceUpdatedEvents(entity, original, _ => { }, cancellationToken);

    Task ProduceUpdatedEvents(TEntity entity, TEntity? original, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    async Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, CancellationToken cancellationToken)
        => await ProduceStatusChangedEvents(entity, original, statusResolver, _ => { }, cancellationToken);

    /// <summary>
    /// Registers a <see cref="GenericStatusChangedEvent{TEntity}"/> and suppresses any
    /// subsequent Updated event for the same entity in the current scope.
    /// </summary>
    Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    async Task ProduceDeletedEvents(TEntity entity, CancellationToken cancellationToken)
        => await ProduceDeletedEvents(entity, _ => { }, cancellationToken);

    Task ProduceDeletedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a custom event to be produced when <see cref="ProduceCustomEvents"/> runs.
    /// <paramref name="configure"/> must set <see cref="IEventDescriptor.EventKey"/>,
    /// <see cref="IEventDescriptor.Summary"/>, and <see cref="IEventDescriptor.Description"/>.
    /// </summary>
    void RegisterCustomEvent(TEntity entity, Action<IEventDescriptor> configure);

    Task ProduceCustomEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken);

    void CustomizeEvents(TEntity entity, EntityEventTypes types, Action<IEventDescriptor> configure);

    void Reset();
}
