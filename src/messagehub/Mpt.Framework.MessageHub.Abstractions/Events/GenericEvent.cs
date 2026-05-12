namespace Mpt.Framework.MessageHub;

/// <summary>
/// Template base for the built-in <see cref="GenericCreatedEvent{TEntity}"/>,
/// <see cref="GenericUpdatedEvent{TEntity}"/>, <see cref="GenericDeletedEvent{TEntity}"/>,
/// <see cref="GenericStatusChangedEvent{TEntity}"/> classes. Holds the module name,
/// current/original entity, and the permissions builder threaded through from the
/// constructor.
/// </summary>
public abstract class GenericEvent<TEntity>
    : PlatformEvent<TEntity> where TEntity : class, IPlatformEntity
{
    private readonly string _moduleName;
    private readonly string _entityName;
    private readonly string _entityKey;
    private readonly PlatformEventPermissionsBuilder _permissionsBuilder;

    private const string _originalKeyPrefix = "original";

    /// <summary>Constructs the event with an optional <paramref name="original"/> baseline (for updates / status changes).</summary>
    protected GenericEvent(string module, TEntity entity, TEntity? original, PlatformEventPermissionsBuilder permissionsBuilder)
    {
        _moduleName = module;
        _entityName = typeof(TEntity).Name;
        _entityKey = ConvertToPathString(_entityName);
        Entity = entity;
        Original = original;
        _permissionsBuilder = permissionsBuilder;
    }

    /// <summary>Constructs the event without an original baseline (for creates / deletes).</summary>
    protected GenericEvent(string module, TEntity entity, PlatformEventPermissionsBuilder permissionsBuilder)
        : this(module, entity, null, permissionsBuilder) { }

    /// <summary>The current entity state.</summary>
    public TEntity Entity { get; }

    /// <summary>The original entity state, when supplied (otherwise <see langword="null"/>).</summary>
    public TEntity? Original { get; }

    /// <inheritdoc/>
    public override string ModuleName => _moduleName;

    /// <inheritdoc/>
    protected override EventMessageObject GetMainObject() => EventObjectFactory.Make(Entity, _entityKey);

    /// <inheritdoc/>
    protected override IEnumerable<EventMessageObject> GetAdditionalObjects()
        => Original != null ? [EventObjectFactory.Make(Original, $"{_originalKeyPrefix}{_entityName}", true)] : [];

    /// <inheritdoc/>
    protected override PlatformEventPermissionsBuilder MakePermissionsBuilder() => _permissionsBuilder;
}
