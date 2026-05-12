namespace Mpt.Framework.MessageHub;

public sealed class GenericUpdatedEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity
{
    public GenericUpdatedEvent(
        string module,
        TEntity data,
        TEntity? original,
        PlatformEventPermissionsBuilder permissionsBuilder)
        : base(module, data, original, permissionsBuilder) { }

    public GenericUpdatedEvent(
        string module,
        TEntity data,
        PlatformEventPermissionsBuilder permissionsBuilder)
        : this(module, data, null, permissionsBuilder) { }

    public override string EventKey => PlatformEventConstants.EVENT_UPDATED;

    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was updated by {{{{actor.name}}}}.";

    protected override string GetSummary(string entityName)
        => $"{entityName} updated";
}
