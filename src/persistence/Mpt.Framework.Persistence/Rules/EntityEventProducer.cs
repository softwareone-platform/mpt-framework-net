using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Internal;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Base implementation of <see cref="IEntityEventProducer{TEntity}"/>. Subclasses override
/// <see cref="ConfigureEvents"/> to declare which actions they participate in. When no
/// <see cref="IPlatformEventEmitter"/> is registered the producer is a silent no-op.
/// </summary>
public class EntityEventProducer<TEntity> : IEntityEventProducer<TEntity>
    where TEntity : class, IPlatformEntity, new()
{
    private EventPolicy<TEntity>? _eventPolicy;
    private readonly string _moduleCode;
    private readonly IPlatformEventEmitter? _eventEmitter;
    private readonly Dictionary<(string EntityId, EntityEventTypes Type), EventDescriptor> _customizations = [];
    private readonly List<(TEntity Entity, Action<IEventDescriptor> Configure)> _registeredCustomEvents = [];

    public EntityEventProducer(IServiceProvider serviceProvider)
    {
        _moduleCode = serviceProvider.GetRequiredService<PersistenceBuilder>().ModuleCode;
        _eventEmitter = serviceProvider.GetService<IPlatformEventEmitter>();
    }

    public bool ShouldProduceOn(EntityAction action)
    {
        if (_eventPolicy == null)
        {
            _eventPolicy = new EventPolicy<TEntity>();
            ConfigureEvents(_eventPolicy);
        }

        return _eventPolicy.IsDefined(action);
    }

    public Task ProduceCreatedEvents(TEntity entity, CancellationToken cancellationToken)
        => ProduceCreatedEvents(entity, _ => { }, cancellationToken);

    public async Task ProduceCreatedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        if (_eventEmitter is null) return;

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

    public Task ProduceUpdatedEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
        => ProduceUpdatedEvents(entity, original, _ => { }, cancellationToken);

    public async Task ProduceUpdatedEvents(TEntity entity, TEntity? original, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        if (_eventEmitter is null) return;

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

    public Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, CancellationToken cancellationToken)
        => ProduceStatusChangedEvents(entity, original, statusResolver, _ => { }, cancellationToken);

    public async Task ProduceStatusChangedEvents(TEntity entity, TEntity? original, Func<TEntity, string> statusResolver, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        if (_eventEmitter is null) return;

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

    public Task ProduceDeletedEvents(TEntity entity, CancellationToken cancellationToken)
        => ProduceDeletedEvents(entity, _ => { }, cancellationToken);

    public async Task ProduceDeletedEvents(TEntity entity, Action<PlatformEvent> configure, CancellationToken cancellationToken)
    {
        if (_eventEmitter is null) return;

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

    public void RegisterCustomEvent(TEntity entity, Action<IEventDescriptor> configure)
    {
        _registeredCustomEvents.Add((entity, configure));
    }

    public async Task ProduceCustomEvents(TEntity entity, TEntity? original, CancellationToken cancellationToken)
    {
        if (_eventEmitter is null)
        {
            _registeredCustomEvents.Clear();
            return;
        }

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

    public void Reset()
    {
        _customizations.Clear();
        _registeredCustomEvents.Clear();
    }

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

    protected string ModuleName => _moduleCode;

    protected virtual void ConfigureEvents(IEventPolicy<TEntity> context) { }

    protected virtual Task ConfigurePermissionsAsync(PlatformEventPermissionsBuilder builder, TEntity entity, TEntity? original, CancellationToken cancellationToken) => Task.CompletedTask;

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
