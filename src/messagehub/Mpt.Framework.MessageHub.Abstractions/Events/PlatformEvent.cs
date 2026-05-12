namespace Mpt.Framework.MessageHub;

/// <summary>
/// Base class for an authored event. Concrete subclasses (typically
/// <see cref="PlatformEvent{TEntity}"/> derivatives) override the abstract members to
/// describe the main subject, summary, and routing of the event; the base class assembles
/// these into a wire-level <see cref="EventMessage"/> via <see cref="MakeMessage"/>.
/// </summary>
public abstract class PlatformEvent : IPlatformEvent
{
    private IEventDescriptor? _customization;

    /// <summary>Routing event key — appears in <see cref="EventMessageRouting.Event"/>.</summary>
    public abstract string EventKey { get; }

    /// <summary>Source module name — appears in <see cref="EventMessageRouting.SourceModule"/>.</summary>
    public abstract string ModuleName { get; }

    /// <summary>Builds the wire-level <see cref="EventMessage"/>.</summary>
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

#pragma warning disable CS0618 // IsTrusted is the [Obsolete] gate that promotes events to EventHints.Incomplete.
        if (!IsTrusted)
        {
            msg.Hints |= EventHints.Incomplete;
        }
#pragma warning restore CS0618

        return msg;
    }

    /// <summary>Customise the next <see cref="MakeMessage"/> output by configuring an <see cref="IEventDescriptor"/>.</summary>
    public void Customize(Action<IEventDescriptor> configure)
    {
        var descriptor = new EventDescriptor();
        configure(descriptor);
        Customize(descriptor);
    }

    /// <summary>Attach a pre-built descriptor so it controls the next <see cref="MakeMessage"/>.</summary>
    public void Customize(IEventDescriptor? customization)
    {
        _customization = customization;
    }

    /// <summary>
    /// <see langword="true"/> when a descriptor has been attached via <see cref="Customize(Action{IEventDescriptor})"/>
    /// or <see cref="Customize(IEventDescriptor?)"/> with <see cref="IEventDescriptor.IsSuppressed"/>
    /// set. The emitter consults this to skip publication.
    /// </summary>
    public bool IsSuppressed => _customization?.IsSuppressed ?? false;

    /// <summary>Builds an additional-entity object for the given entity.</summary>
    protected static EventMessageObject MakeAdditionalEntityObject<TEntity>(
        TEntity entity,
        string key)
        where TEntity : IPlatformEntity
        => EventObjectFactory.MakeAdditional(entity, key);

    /// <summary>Builds an additional-entity object from custom data.</summary>
    protected static EventMessageObject MakeAdditionalEntityObject(
        object data,
        string id,
        string key,
        string entityName)
        => EventObjectFactory.MakeAdditionalCustom(data, id, key, entityName);

    /// <summary>Builds a main or original entity object.</summary>
    protected static EventMessageObject MakeEntityObject<TEntity>(
        TEntity entity,
        string key,
        bool isOriginalValue = false)
        where TEntity : IPlatformEntity
        => EventObjectFactory.Make(entity, key, isOriginalValue);

    /// <summary>Builds an entity object with an explicit category.</summary>
    protected static EventMessageObject MakeEntityObject<TEntity>(
        TEntity entity,
        string key,
        EventMessageObjectCategory category)
        where TEntity : IPlatformEntity
        => EventObjectFactory.Make(entity, key, category);

    /// <summary>camelCase the first character of <paramref name="source"/> for event paths.</summary>
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

    /// <summary>Returns the subject of this event (Category = CurrentEntity).</summary>
    protected abstract EventMessageObject GetMainObject();

    /// <summary>Returns objects to append after the main object.</summary>
    protected virtual IEnumerable<EventMessageObject> GetAdditionalObjects() => [];

    /// <summary>Returns the display name of the subject entity.</summary>
    protected abstract string GetEntityName();

    /// <summary>Builds the summary line for the event.</summary>
    protected abstract string GetSummary(string entityName);

    /// <summary>Builds the description line for the event.</summary>
    protected abstract string GetDescription(string entityName, string entityKey);

    /// <summary>Constructs the per-event permissions builder.</summary>
    protected virtual PlatformEventPermissionsBuilder MakePermissionsBuilder() => new();

    /// <summary>Subclasses can populate <paramref name="builder"/> with principal-access entries.</summary>
    protected virtual void ConfigurePermissions(PlatformEventPermissionsBuilder builder) { }

    /// <summary>
    /// When <see langword="false"/>, the produced message has <see cref="EventHints.Incomplete"/>
    /// set. <see cref="GenericDeletedEvent{TEntity}"/> overrides this to <see langword="false"/>.
    /// </summary>
    [Obsolete("Use Hints instead")]
    protected virtual bool IsTrusted => true;

    /// <summary>Default hint flags applied when no customisation overrides them.</summary>
    public virtual EventHints Hints { get; set; }

    /// <summary>Stream the event publishes to. Default <see cref="StreamTypes.Events"/>.</summary>
    protected virtual StreamTypes TargetStream => StreamTypes.Events;

    /// <summary>Optional list of modules the event is targeted to.</summary>
    public List<string> TargetModules { get; set; } = [];

    /// <summary>Optional session id override. Defaults to the main object's id.</summary>
    protected virtual string? GetSessionId() => null;

    /// <summary>Optional partition key override.</summary>
    protected virtual string? GetPartitionKey() => null;
}

/// <summary>
/// Convenience <see cref="PlatformEvent"/> base for events scoped to a single
/// <typeparamref name="TEntity"/>. Overrides <see cref="PlatformEvent.GetEntityName"/>
/// to use the CLR type name; subclasses can override again to return a friendlier label.
/// </summary>
public abstract class PlatformEvent<TEntity> : PlatformEvent
    where TEntity : IPlatformEntity
{
    /// <summary>Returns <c>typeof(TEntity).Name</c>. Override to customise.</summary>
    protected override string GetEntityName() => typeof(TEntity).Name;
}
