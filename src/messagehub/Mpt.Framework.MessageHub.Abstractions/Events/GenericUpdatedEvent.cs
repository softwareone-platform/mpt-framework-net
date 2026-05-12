namespace Mpt.Framework.MessageHub;

/// <summary>Lifecycle event signalling that <typeparamref name="TEntity"/> was just updated.</summary>
public sealed class GenericUpdatedEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity
{
    /// <summary>Constructs the event with an <paramref name="original"/> baseline for diff reconstruction.</summary>
    public GenericUpdatedEvent(
        string module,
        TEntity data,
        TEntity? original,
        PlatformEventPermissionsBuilder permissionsBuilder)
        : base(module, data, original, permissionsBuilder) { }

    /// <summary>Constructs the event without an original baseline.</summary>
    public GenericUpdatedEvent(
        string module,
        TEntity data,
        PlatformEventPermissionsBuilder permissionsBuilder)
        : this(module, data, null, permissionsBuilder) { }

    /// <inheritdoc/>
    public override string EventKey => PlatformEventConstants.EVENT_UPDATED;

    /// <inheritdoc/>
    protected override string GetDescription(string entityName, string entityKey)
        => $"The {entityName} {{{{{entityKey}.id}}}} was updated by {{{{actor.name}}}}.";

    /// <inheritdoc/>
    protected override string GetSummary(string entityName)
        => $"{entityName} updated";
}
