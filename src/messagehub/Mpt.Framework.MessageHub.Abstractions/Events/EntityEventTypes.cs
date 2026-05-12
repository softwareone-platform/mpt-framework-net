namespace Mpt.Framework.MessageHub;

/// <summary>
/// Categorises lifecycle events produced by an <see cref="IEntityEventProducer{TEntity}"/>.
/// </summary>
/// <remarks>
/// Duplicates <c>Mpt.Framework.Persistence.EntityEventTypes</c> for the MessageHub.Events
/// authoring layer. Same values, separate namespaces — see <see cref="EntityAction"/> for
/// the rationale.
/// </remarks>
[Flags]
public enum EntityEventTypes
{
    /// <summary>No event categories enabled.</summary>
    None = 0,

    /// <summary>Entity was just created.</summary>
    Created = 1 << 0,

    /// <summary>Entity was just updated.</summary>
    Updated = 1 << 1,

    /// <summary>Entity was just deleted.</summary>
    Deleted = 1 << 2,

    /// <summary>Entity's status changed as part of an update.</summary>
    StatusChanged = 1 << 3,

    /// <summary>All categories enabled.</summary>
    All = Created | Updated | Deleted | StatusChanged,
}
