namespace Mpt.Framework.MessageHub;

/// <summary>
/// Base class for an authored event. Subclasses override the abstract members to
/// describe the main subject, summary, and routing of the event; <see cref="MakeMessage"/>
/// assembles them into a wire-level <see cref="EventMessage"/>.
/// </summary>
public abstract class PlatformEvent : IPlatformEvent
{
    private IEventDescriptor? _customization;

    public abstract string EventKey { get; }

    public abstract string ModuleName { get; }

    public EventMessage MakeMessage()
    {
        var mainObject = GetMainObject();
        var entityTypeName = mainObject.Type ?? "unknown";
        var entityName = _customization?.EntityName ?? GetEntityName();
        var entityKey = _customization?.EntityKey ?? mainObject.Key;

        if (mainObject.Category != EventMessageObjectCategory.CurrentEntity)
        {
            throw new InvalidOperationException($"Main event object must be of type {EventMessageObjectCategory.CurrentEntity}");
        }

        var objects = new List<EventMessageObject> { mainObject };
        objects.AddRange(GetAdditionalObjects());

        if (_customization?.AdditionalObjects != null)
        {
            objects.AddRange(_customization.AdditionalObjects);
        }

        var groupByKey = objects.GroupBy(g => g.Key);

        foreach (var byKey in groupByKey)
        {
            if (byKey.Key == null)
            {
                throw new InvalidOperationException("Null event object key detected");
            }

            if (byKey.Count() > 1)
            {
                throw new InvalidOperationException($"Duplicate event object key: {byKey.Key}");
            }
        }

        var permissionsBuilder = MakePermissionsBuilder();
        ConfigurePermissions(permissionsBuilder);

        var msg = new EventMessage
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Hints = _customization?.Hints ?? Hints,
            Routing = MakeRouting(entityTypeName),
            Objects = objects,
            Info = new EventMessageInfo
            {
                Summary = _customization?.Summary
                    ?? GetSummary(entityName),
                Description = _customization?.Description
                    ?? GetDescription(entityName, entityKey)
            },
            SessionId = GetSessionId() ?? mainObject.Id,
            PartitionKey = GetPartitionKey()
        };

#pragma warning disable CS0618
        if (!IsTrusted)
        {
            msg.Hints |= EventHints.Incomplete;
        }
#pragma warning restore CS0618

        return msg;
    }

    public void Customize(Action<IEventDescriptor> configure)
    {
        var descriptor = new EventDescriptor();
        configure(descriptor);
        Customize(descriptor);
    }

    public void Customize(IEventDescriptor? customization)
    {
        _customization = customization;
    }

    public bool IsSuppressed => _customization?.IsSuppressed ?? false;

    protected static EventMessageObject MakeAdditionalEntityObject<TEntity>(
        TEntity entity,
        string key)
        where TEntity : IPlatformEntity
        => EventObjectFactory.MakeAdditional(entity, key);

    protected static EventMessageObject MakeAdditionalEntityObject(
        object data,
        string id,
        string key,
        string entityName)
        => EventObjectFactory.MakeAdditionalCustom(data, id, key, entityName);

    protected static EventMessageObject MakeEntityObject<TEntity>(
        TEntity entity,
        string key,
        bool isOriginalValue = false)
        where TEntity : IPlatformEntity
        => EventObjectFactory.Make(entity, key, isOriginalValue);

    protected static EventMessageObject MakeEntityObject<TEntity>(
        TEntity entity,
        string key,
        EventMessageObjectCategory category)
        where TEntity : IPlatformEntity
        => EventObjectFactory.Make(entity, key, category);

    protected static string ConvertToPathString(string source) => source.ToEventPathString();

    private EventMessageRouting MakeRouting(string entityTypeName)
    {
        return new EventMessageRouting
        {
            Stream = TargetStream,
            SourceModule = ModuleName,
            Entity = entityTypeName,
            Event = _customization?.EventKey ?? EventKey,
            TargetModules = TargetModules
        };
    }

    protected abstract EventMessageObject GetMainObject();

    protected virtual IEnumerable<EventMessageObject> GetAdditionalObjects() => [];

    protected abstract string GetEntityName();

    protected abstract string GetSummary(string entityName);

    protected abstract string GetDescription(string entityName, string entityKey);

    protected virtual PlatformEventPermissionsBuilder MakePermissionsBuilder() => new();

    protected virtual void ConfigurePermissions(PlatformEventPermissionsBuilder builder) { }

    /// <summary>
    /// When <see langword="false"/>, the produced message has <see cref="EventHints.Incomplete"/>
    /// OR'd into <see cref="EventMessage.Hints"/>.
    /// </summary>
    [Obsolete("Use Hints instead")]
    protected virtual bool IsTrusted => true;

    public virtual EventHints Hints { get; set; }

    protected virtual StreamTypes TargetStream => StreamTypes.Events;

    public List<string> TargetModules { get; set; } = [];

    protected virtual string? GetSessionId() => null;

    protected virtual string? GetPartitionKey() => null;
}

public abstract class PlatformEvent<TEntity> : PlatformEvent
    where TEntity : IPlatformEntity
{
    protected override string GetEntityName() => typeof(TEntity).Name;
}
