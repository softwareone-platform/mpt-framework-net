namespace Mpt.Framework.MessageHub;

/// <summary>
/// Well-known <see cref="EventMessageRouting.Event"/> values produced by the
/// built-in <c>Generic*Event&lt;TEntity&gt;</c> classes.
/// </summary>
public static class PlatformEventConstants
{
    /// <summary>Event key for <see cref="GenericCreatedEvent{TEntity}"/>.</summary>
    public const string EVENT_CREATED = "created";

    /// <summary>Event key for <see cref="GenericUpdatedEvent{TEntity}"/>.</summary>
    public const string EVENT_UPDATED = "updated";

    /// <summary>Event key for <see cref="GenericDeletedEvent{TEntity}"/>.</summary>
    public const string EVENT_DELETED = "deleted";

    /// <summary>Event key for <see cref="GenericStatusChangedEvent{TEntity}"/>.</summary>
    public const string EVENT_STATUS_CHANGED = "status_changed";
}
