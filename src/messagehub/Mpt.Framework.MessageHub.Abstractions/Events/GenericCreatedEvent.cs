namespace Mpt.Framework.MessageHub;

/// <summary>Lifecycle event signalling that <typeparamref name="TEntity"/> was just created.</summary>
public sealed class GenericCreatedEvent<TEntity>(
    string module,
    TEntity data,
    PlatformEventPermissionsBuilder permissionsBuilder)
    : GenericEvent<TEntity>(module, data, permissionsBuilder)
    where TEntity : class, IPlatformEntity
{
    /// <inheritdoc/>
    public override string EventKey => PlatformEventConstants.EVENT_CREATED;

    /// <inheritdoc/>
    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was created by {{{{actor.name}}}}.";

    /// <inheritdoc/>
    protected override string GetSummary(string entityName)
        => $"{entityName} created";
}
