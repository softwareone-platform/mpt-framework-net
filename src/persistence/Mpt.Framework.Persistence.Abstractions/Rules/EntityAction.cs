namespace Mpt.Framework.Persistence;

/// <summary>
/// The three lifecycle actions tracked by the persistence engine.
/// </summary>
public enum EntityAction
{
    /// <summary>The entity is being added to the unit of work.</summary>
    Create,

    /// <summary>The entity is being modified in the unit of work.</summary>
    Update,

    /// <summary>The entity is being removed from the unit of work.</summary>
    Delete,
}
