namespace Mpt.Framework.MessageHub;

public sealed class GenericDeletedEvent<TEntity>(
    string module,
    TEntity data,
    PlatformEventPermissionsBuilder permissionsBuilder)
    : GenericEvent<TEntity>(module, data, permissionsBuilder)
    where TEntity : class, IPlatformEntity
{
    public override string EventKey => PlatformEventConstants.EVENT_DELETED;

    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was deleted by {{{{actor.name}}}}.";

    protected override string GetSummary(string entityName)
        => $"{entityName} deleted";

    [Obsolete("Use Hints instead")]
    protected override bool IsTrusted => false;

    protected sealed override void ConfigurePermissions(PlatformEventPermissionsBuilder builder)
    {
    }
}
