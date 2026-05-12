using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// Base implementation of <see cref="IEntityEventProducer{TEntity}"/>. Subclasses override
/// <see cref="ConfigureEvents"/> to declare which <see cref="EntityAction"/> values they
/// react to, optionally override <see cref="ConfigurePermissionsAsync"/> to attach
/// principal access to produced events, and optionally override <see cref="OnEventProduced"/>
/// to attach additional objects or customisations before the emitter sees the event.
/// </summary>
public class EntityEventProducer<TEntity> : IEntityEventProducer<TEntity>
    where TEntity : class, IPlatformEntity, new()
{
    private EventPolicy<TEntity>? _eventPolicy;
    private readonly string _moduleCode;
    private readonly IPlatformEventEmitter _eventEmitter;
    private readonly Dictionary<(string EntityId, EntityEventTypes Type), EventDescriptor> _customizations = [];
    private readonly List<(TEntity Entity, Action<IEventDescriptor> Configure)> _registeredCustomEvents = [];

    /// <summary>Constructs the producer, resolving the module code from <see cref="MessageHubBuilder"/> and the shared emitter from DI.</summary>
    public EntityEventProducer(IServiceProvider serviceProvider)
    {
        _moduleCode = serviceProvider.GetRequiredService<MessageHubBuilder>().ModuleCode;
        _eventEmitter = serviceProvider.GetRequiredService<IPlatformEventEmitter>();
    }

    /// <inheritdoc/>
    public bool ShouldProduceOn(EntityAction action)
    {
        if (_eventPolicy == null)
        {
            _eventPolicy = new EventPolicy<TEntity>();
            ConfigureEvents(_eventPolicy);
        }

        return _eventPolicy.IsDefined(action);
    }

    /// <summary>Convenience overload that registers a created event with no extra configuration. Class-level forwarder for the default-interface overload.</summary>
    public Task ProduceCreatedEvents(TEntity entity, CancellationToken cancellationToken)
        => ProduceCreatedEvents(entity, _ => { }, cancellationToken);

    /// <summary>Convenience overload that registers an updated event with no extra configuration. Class-level forwarder for the default-interface overload.</summary>
    public Task ProduceUpdatedEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
        => ProduceUpdatedEvents(entity, original, _ => { }, cancellationToken);

    /// <summary>Convenience overload that registers a status-changed event with no extra configuration. Class-level forwarder for the default-interface overload.</summary>
    public Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, CancellationToken cancellationToken)
        => ProduceStatusChangedEvents(entity, original, statusResolver, _ => { }, cancellationToken);

    /// <summary>Convenience overload that registers a deleted event with no extra configuration. Class-level forwarder for the default-interface overload.</summary>
    public Task ProduceDeletedEvents(TEntity entity, CancellationToken cancellationToken)
        => ProduceDeletedEvents(entity, _ => { }, cancellationToken);

    /// <inheritdoc/>
    public async Task ProduceCreatedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        var customization = GetCustomization(entity, EntityEventTypes.Created);

        if (customization != null && customization.IsSuppressed)
            return;

        var permissionsBuilder = await GetPermissionBuilder(entity, null, cancellationToken);
        var @event = new GenericCreatedEvent<TEntity>(_moduleCode, entity, permissionsBuilder);
        OnEventProduced(@event, entity, null);
        configure.Invoke(@event);
        @event.Customize(customization);
        _eventEmitter.Register(@event);
    }

    /// <inheritdoc/>
    public async Task ProduceUpdatedEvents(TEntity entity, TEntity? original, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        var customization = GetCustomization(entity, EntityEventTypes.Updated);

        if (customization != null && customization.IsSuppressed)
            return;

        var permissionsBuilder = await GetPermissionBuilder(entity, original, cancellationToken);
        var @event = new GenericUpdatedEvent<TEntity>(_moduleCode, entity, original, permissionsBuilder);
        OnEventProduced(@event, entity, original);
        configure.Invoke(@event);
        @event.Customize(customization);
        _eventEmitter.Register(@event);
    }

    /// <inheritdoc/>
    public async Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        var customization = GetCustomization(entity, EntityEventTypes.StatusChanged);

        if (customization != null && customization.IsSuppressed)
            return;

        var permissionsBuilder = await GetPermissionBuilder(entity, original, cancellationToken);
        var @event = new GenericStatusChangedEvent<TEntity>(_moduleCode, entity, original, permissionsBuilder, statusResolver);
        OnEventProduced(@event, entity, original);
        configure.Invoke(@event);
        @event.Customize(customization);
        _eventEmitter.Register(@event);

        // Status change supersedes a regular Updated event for the same entity in this scope.
        CustomizeEvents(entity, EntityEventTypes.Updated, t => t.IsSuppressed = true);
    }

    /// <inheritdoc/>
    public async Task ProduceDeletedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        var customization = GetCustomization(entity, EntityEventTypes.Deleted);

        if (customization != null && customization.IsSuppressed)
            return;

        var data = new TEntity() { Id = entity.Id };
        var permissionsBuilder = await GetPermissionBuilder(entity, null, cancellationToken);
        var @event = new GenericDeletedEvent<TEntity>(_moduleCode, data, permissionsBuilder);
        OnEventProduced(@event, entity, null);
        configure.Invoke(@event);
        @event.Customize(customization);
        _eventEmitter.Register(@event);
    }

    /// <inheritdoc/>
    public void RegisterCustomEvent(TEntity entity, Action<IEventDescriptor> configure)
    {
        _registeredCustomEvents.Add((entity, configure));
    }

    /// <inheritdoc/>
    public async Task ProduceCustomEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
    {
        foreach (var (registeredEntity, configure) in _registeredCustomEvents)
        {
            if (registeredEntity.Id != entity.Id)
                continue;

            var descriptor = new EventDescriptor();
            configure.Invoke(descriptor);

            if (string.IsNullOrEmpty(descriptor.EventKey))
                throw new InvalidOperationException("EventKey is required");

            if (descriptor.Summary is null)
                throw new InvalidOperationException("Summary is required");

            if (descriptor.Description is null)
                throw new InvalidOperationException("Description is required");

            var permissionsBuilder = await GetPermissionBuilder(entity, original, cancellationToken);
            var @event = new CustomEvent<TEntity>(_moduleCode, entity, original, permissionsBuilder);
            @event.Customize(descriptor);
            OnEventProduced(@event, entity, original);
            _eventEmitter.Register(@event);
        }

        _registeredCustomEvents.Clear();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _customizations.Clear();
        _registeredCustomEvents.Clear();
    }

    /// <inheritdoc/>
    public void CustomizeEvents(TEntity entity, EntityEventTypes types, Action<IEventDescriptor> configure)
    {
        foreach (var type in SplitEventTypes(types))
        {
            if (!_customizations.TryGetValue((entity.Id, type), out var customization))
            {
                customization = new EventDescriptor();
            }

            configure(customization);
            _customizations[(entity.Id, type)] = customization;
        }
    }

    /// <summary>The module code stamped onto every emitted event's <see cref="EventMessageRouting.SourceModule"/>.</summary>
    protected string ModuleName => _moduleCode;

    /// <summary>Subclasses call <see cref="IEventPolicy{TEntity}.Define"/> here for each action they react to.</summary>
    protected virtual void ConfigureEvents(IEventPolicy<TEntity> context) { }

    /// <summary>Subclasses can populate <paramref name="builder"/> with per-event principal access.</summary>
    protected virtual Task ConfigurePermissionsAsync(PlatformEventPermissionsBuilder builder, TEntity entity, TEntity? original, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Hook for subclasses to attach extras (additional objects, customisation) just before the event is registered with the emitter.</summary>
    protected virtual void OnEventProduced(PlatformEvent platformEvent, TEntity entity, TEntity? original) { }

    private async Task<PlatformEventPermissionsBuilder> GetPermissionBuilder(TEntity entity, TEntity? original, CancellationToken cancellationToken)
    {
        var builder = new PlatformEventPermissionsBuilder();
        await ConfigurePermissionsAsync(builder, entity, original, cancellationToken);
        return builder;
    }

    private EventDescriptor? GetCustomization(TEntity entity, EntityEventTypes type)
    {
        return _customizations.GetValueOrDefault((entity.Id, type));
    }

    private static IEnumerable<EntityEventTypes> SplitEventTypes(EntityEventTypes types)
    {
        foreach (var value in Enum.GetValues<EntityEventTypes>())
        {
            if (value != EntityEventTypes.None && value != EntityEventTypes.All && types.HasFlag(value))
                yield return value;
        }
    }
}
