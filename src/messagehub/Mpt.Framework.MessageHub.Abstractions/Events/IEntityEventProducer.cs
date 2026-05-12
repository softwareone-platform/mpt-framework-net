namespace Mpt.Framework.MessageHub;

/// <summary>
/// Non-generic marker. Lets DI containers resolve every registered
/// <see cref="IEntityEventProducer{TEntity}"/> through a single interface.
/// </summary>
public interface IEntityEventProducer;

/// <summary>
/// Per-entity event producer that knows how to turn entity lifecycle actions into
/// <see cref="GenericCreatedEvent{TEntity}"/> / <see cref="GenericUpdatedEvent{TEntity}"/>
/// / <see cref="GenericDeletedEvent{TEntity}"/> / <see cref="GenericStatusChangedEvent{TEntity}"/>
/// instances and register them with <see cref="IPlatformEventEmitter"/>. Concrete
/// subclasses typically derive from <c>EntityEventProducer&lt;TEntity&gt;</c> in the
/// engine package and override <c>ConfigureEvents</c> / <c>ConfigurePermissionsAsync</c>.
/// </summary>
/// <remarks>
/// Note: <c>Mpt.Framework.Persistence</c> defines its own <see cref="IEntityEventProducer{TEntity}"/>
/// with a different shape (single <c>ProduceAsync</c> method that yields
/// <see cref="EventMessage"/> instances). Both interfaces have the same simple name but
/// live in separate namespaces — when you reference both, fully-qualify at use sites.
/// </remarks>
public interface IEntityEventProducer<TEntity> : IEntityEventProducer
    where TEntity : class, IPlatformEntity, new()
{
    /// <summary>
    /// Returns <see langword="true"/> if the producer participates in <paramref name="action"/>.
    /// </summary>
    bool ShouldProduceOn(EntityAction action);

    /// <summary>Convenience overload that registers a created event with no extra configuration.</summary>
    async Task ProduceCreatedEvents(TEntity entity, CancellationToken cancellationToken)
        => await ProduceCreatedEvents(entity, _ => { }, cancellationToken);

    /// <summary>Registers a <see cref="GenericCreatedEvent{TEntity}"/> with the emitter.</summary>
    Task ProduceCreatedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload that registers an updated event with no extra configuration.</summary>
    async Task ProduceUpdatedEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
        => await ProduceUpdatedEvents(entity, original, _ => { }, cancellationToken);

    /// <summary>Registers a <see cref="GenericUpdatedEvent{TEntity}"/> with the emitter.</summary>
    Task ProduceUpdatedEvents(TEntity entity, TEntity? original, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload that registers a status-changed event with no extra configuration.</summary>
    async Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, CancellationToken cancellationToken)
        => await ProduceStatusChangedEvents(entity, original, statusResolver, _ => { }, cancellationToken);

    /// <summary>
    /// Registers a <see cref="GenericStatusChangedEvent{TEntity}"/> with the emitter.
    /// Implementations should also suppress any subsequent Updated event for the same
    /// entity in the current scope — status change supersedes update.
    /// </summary>
    Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, Action<PlatformEvent> configure, CancellationToken cancellationToken);

    /// <summary>Convenience overload that registers a deleted event with no extra configuration.</summary>
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
