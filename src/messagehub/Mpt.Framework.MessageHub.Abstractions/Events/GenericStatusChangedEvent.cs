namespace Mpt.Framework.MessageHub;

/// <summary>
/// Lifecycle event signalling that an entity's status field transitioned to a new value.
/// </summary>
public sealed class GenericStatusChangedEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity
{
    private readonly TEntity _data;
    private readonly Func<TEntity, string> _statusResolver;

    /// <summary>Constructs the event with optional <paramref name="original"/> baseline.</summary>
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

    /// <summary>Constructs the event without an original baseline.</summary>
    public GenericStatusChangedEvent(
        string module,
        TEntity data,
        PlatformEventPermissionsBuilder permissionsBuilder,
        Func<TEntity, string> statusResolver)
        : this(module, data, null, permissionsBuilder, statusResolver) { }

    /// <inheritdoc/>
    public override string EventKey => PlatformEventConstants.EVENT_STATUS_CHANGED;

    /// <inheritdoc/>
    protected override string GetDescription(string entityName, string entityKey)
        => $"{entityName} {{{{{entityKey}.id}}}} status was changed to {_statusResolver(_data)} by {{{{actor.name}}}} ({{{{actor.id}}}}).";

    /// <inheritdoc/>
    protected override string GetSummary(string entityName)
        => $"{entityName} status changed to {_statusResolver(_data)}";
}
