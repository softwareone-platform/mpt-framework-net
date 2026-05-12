using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic marker. Lets DI containers locate every registered
/// <see cref="IEntityEventProducer{TEntity}"/> through a single base type.
/// </summary>
public interface IEntityEventProducer;

/// <summary>
/// Per-entity event producer. Invoked by <see cref="IRepository{TEntity}"/> in the
/// after-save phase to construct lifecycle events (created / updated / deleted /
/// status-changed) and any registered custom events, then register them with the
/// shared <see cref="IPlatformEventEmitter"/>. The unit of work flushes the emitter
/// after every repository has produced.
/// </summary>
/// <remarks>
/// Concrete implementations typically derive from <c>EntityEventProducer&lt;TEntity&gt;</c>
/// in the Persistence engine package and override <c>ConfigureEvents</c> to declare
/// which actions the producer participates in, plus optionally
/// <c>ConfigurePermissionsAsync</c> and <c>OnEventProduced</c>.
/// </remarks>
public interface IEntityEventProducer<TEntity> : IEntityEventProducer
    where TEntity : class, IPlatformEntity, new()
{
    /// <summary>
    /// Returns <see langword="true"/> if the producer participates in <paramref name="action"/>.
    /// The repository skips the corresponding <c>Produce*Events</c> call when this returns false.
    /// </summary>
    bool ShouldProduceOn(EntityAction action);

    /// <summary>Convenience overload — registers a created event without extra configuration.</summary>
    async Task ProduceCreatedEvents(TEntity entity, CancellationToken cancellationToken)
        => await ProduceCreatedEvents(entity, _ => { }, cancellationToken);

    /// <summary>Registers a <see cref="GenericCreatedEvent{TEntity}"/> with the emitter.</summary>
    Task ProduceCreatedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload — registers an updated event without extra configuration.</summary>
    async Task ProduceUpdatedEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
        => await ProduceUpdatedEvents(entity, original, _ => { }, cancellationToken);

    /// <summary>Registers a <see cref="GenericUpdatedEvent{TEntity}"/> with the emitter.</summary>
    Task ProduceUpdatedEvents(TEntity entity, TEntity? original, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload — registers a status-changed event without extra configuration.</summary>
    async Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, CancellationToken cancellationToken)
        => await ProduceStatusChangedEvents(entity, original, statusResolver, _ => { }, cancellationToken);

    /// <summary>
    /// Registers a <see cref="GenericStatusChangedEvent{TEntity}"/> with the emitter.
    /// Implementations also suppress any subsequent Updated event for the same entity
    /// in the current scope — status change supersedes update.
    /// </summary>
    Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload — registers a deleted event without extra configuration.</summary>
    async Task ProduceDeletedEvents(TEntity entity, CancellationToken cancellationToken)
        => await ProduceDeletedEvents(entity, _ => { }, cancellationToken);

    /// <summary>Registers a <see cref="GenericDeletedEvent{TEntity}"/> with the emitter.</summary>
    Task ProduceDeletedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a custom event for <paramref name="entity"/> to be produced later when
    /// <see cref="ProduceCustomEvents"/> is called. Use for events that don't fit the
    /// standard CRUD categories. The <paramref name="configure"/> action must set
    /// <see cref="IEventDescriptor.EventKey"/>, <see cref="IEventDescriptor.Summary"/>,
    /// and <see cref="IEventDescriptor.Description"/>.
    /// </summary>
    void RegisterCustomEvent(TEntity entity, Action<IEventDescriptor> configure);

    /// <summary>
    /// Produces every custom event previously registered for <paramref name="entity"/> via
    /// <see cref="RegisterCustomEvent"/>. Clears the registration buffer afterwards.
    /// </summary>
    Task ProduceCustomEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken);

    /// <summary>
    /// Adjusts the descriptor used the next time the producer constructs events of the
    /// given <paramref name="types"/> for <paramref name="entity"/>. Flags may be
    /// combined (<c>Created | Updated</c>); the configuration is applied independently
    /// to each type.
    /// </summary>
    void CustomizeEvents(TEntity entity, EntityEventTypes types, Action<IEventDescriptor> configure);

    /// <summary>Clears any cached customisations and pending custom-event registrations.</summary>
    void Reset();
}
