namespace Mpt.Framework.MessageHub;

public abstract class GenericEvent<TEntity>
    : PlatformEvent<TEntity> where TEntity : class, IPlatformEntity
{
    private readonly string _moduleName;
    private readonly string _entityName;
    private readonly string _entityKey;
    private readonly PlatformEventPermissionsBuilder _permissionsBuilder;

    private const string _originalKeyPrefix = "original";

    protected GenericEvent(string module, TEntity entity, TEntity? original, PlatformEventPermissionsBuilder permissionsBuilder)
    {
        _moduleName = module;
        _entityName = typeof(TEntity).Name;
        _entityKey = ConvertToPathString(_entityName);
        Entity = entity;
        Original = original;
        _permissionsBuilder = permissionsBuilder;
    }

    protected GenericEvent(string module, TEntity entity, PlatformEventPermissionsBuilder permissionsBuilder)
        : this(module, entity, null, permissionsBuilder) { }

    public TEntity Entity { get; }

    public TEntity? Original { get; }

    public override string ModuleName => _moduleName;

    protected override EventMessageObject GetMainObject() => EventObjectFactory.Make(Entity, _entityKey);

    protected override IEnumerable<EventMessageObject> GetAdditionalObjects()
        => Original != null ? [EventObjectFactory.Make(Original, $"{_originalKeyPrefix}{_entityName}", true)] : [];

    protected override PlatformEventPermissionsBuilder MakePermissionsBuilder() => _permissionsBuilder;
}
