namespace Mpt.Framework.MessageHub;

/// <summary>
/// The three lifecycle actions an <see cref="IEntityEventProducer{TEntity}"/> can react to.
/// </summary>
/// <remarks>
/// Duplicates <c>Mpt.Framework.Persistence.EntityAction</c> for the MessageHub.Events authoring layer.
/// The two enums have identical members but live in separate namespaces because the project
/// dependency direction is Persistence → MessageHub and never the reverse.
/// </remarks>
public enum EntityAction
{
    /// <summary>The entity is being added.</summary>
    Create,

    /// <summary>The entity is being modified.</summary>
    Update,

    /// <summary>The entity is being removed.</summary>
    Delete,
}
