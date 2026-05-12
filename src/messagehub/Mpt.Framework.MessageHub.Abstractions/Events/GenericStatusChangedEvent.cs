namespace Mpt.Framework.MessageHub;

public sealed class GenericStatusChangedEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity
{
    private readonly TEntity _data;
    private readonly Func<TEntity, string> _statusResolver;

    public GenericStatusChangedEvent(
        string module,
        TEntity data,
        TEntity? original,
        PlatformEventPermissionsBuilder permissionsBuilder,
        Func<TEntity, string> statusResolver)
        : base(module, data, original, permissionsBuilder)
    {
        _data = data;
        _statusResolver = statusResolver;
    }

    public GenericStatusChangedEvent(
        string module,
        TEntity data,
        PlatformEventPermissionsBuilder permissionsBuilder,
        Func<TEntity, string> statusResolver)
        : this(module, data, null, permissionsBuilder, statusResolver) { }

    public override string EventKey => PlatformEventConstants.EVENT_STATUS_CHANGED;

    protected override string GetDescription(string entityName, string entityKey)
        => $"{entityName} {{{{{entityKey}.id}}}} status was changed to {_statusResolver(_data)} by {{{{actor.name}}}} ({{{{actor.id}}}}).";

    protected override string GetSummary(string entityName)
        => $"{entityName} status changed to {_statusResolver(_data)}";
}
