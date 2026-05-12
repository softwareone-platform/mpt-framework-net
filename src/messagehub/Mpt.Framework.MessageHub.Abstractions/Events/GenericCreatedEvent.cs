namespace Mpt.Framework.MessageHub;

public sealed class GenericCreatedEvent<TEntity>(
    string module,
    TEntity data,
    PlatformEventPermissionsBuilder permissionsBuilder)
    : GenericEvent<TEntity>(module, data, permissionsBuilder)
    where TEntity : class, IPlatformEntity
{
    public override string EventKey => PlatformEventConstants.EVENT_CREATED;

    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was created by {{{{actor.name}}}}.";

    protected override string GetSummary(string entityName)
        => $"{entityName} created";
}
