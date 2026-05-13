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

    /// <summary>
    /// Marks the deleted-event message as <see cref="EventHints.Incomplete"/> by default —
    /// only the entity ID survives the delete, so downstream consumers should fetch any
    /// additional context elsewhere. Callers can replace the value if needed.
    /// </summary>
    public override EventHints Hints { get; set; } = EventHints.Incomplete;

    /// <summary>
    /// Intentionally empty — a deleted entity has no permissions to project: the
    /// authorisation envelope from the originating create/update events governs who
    /// receives the deletion. Override this in a derived event if you need to override
    /// that behaviour for a specific entity type.
    /// </summary>
    protected sealed override void ConfigurePermissions(PlatformEventPermissionsBuilder builder)
    {
        // No-op by design: deleted entities project no new permissions. See the XML doc above.
    }
}
