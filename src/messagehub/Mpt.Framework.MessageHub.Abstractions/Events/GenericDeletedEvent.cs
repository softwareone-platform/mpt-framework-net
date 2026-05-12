namespace Mpt.Framework.MessageHub;

/// <summary>
/// Lifecycle event signalling that <typeparamref name="TEntity"/> was just deleted.
/// Carries only the entity id, and is automatically marked
/// <see cref="EventHints.Incomplete"/> because consumers cannot rely on the wire payload
/// reflecting the deleted entity's full state.
/// </summary>
public sealed class GenericDeletedEvent<TEntity>(
    string module,
    TEntity data,
    PlatformEventPermissionsBuilder permissionsBuilder)
    : GenericEvent<TEntity>(module, data, permissionsBuilder)
    where TEntity : class, IPlatformEntity
{
    /// <inheritdoc/>
    public override string EventKey => PlatformEventConstants.EVENT_DELETED;

    /// <inheritdoc/>
    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was deleted by {{{{actor.name}}}}.";

    /// <inheritdoc/>
    protected override string GetSummary(string entityName)
        => $"{entityName} deleted";

    /// <inheritdoc/>
    [Obsolete("Use Hints instead")]
    protected override bool IsTrusted => false;

    /// <inheritdoc/>
    protected sealed override void ConfigurePermissions(PlatformEventPermissionsBuilder builder)
    {
        // No permissions required for deleted events.
    }
}
