namespace Mpt.Framework.Persistence;

/// <summary>
/// Configures which entity actions (Create / Update / Delete or named custom action)
/// are permitted for which caller roles. Used by <see cref="IEntityConfiguration{TEntity}"/>.
/// </summary>
public interface IActionPolicy<TEntity>
{
    /// <summary>
    /// Permits <paramref name="action"/> for callers holding any of <paramref name="roles"/>.
    /// Pass no roles to permit the action for any caller.
    /// </summary>
    IActionPolicy<TEntity> Define(EntityAction action, params string[] roles)
        => Define(action.ToString(), roles);

    /// <summary>
    /// Permits the named action for callers holding any of <paramref name="roles"/>.
    /// Pass no roles to permit the action for any caller.
    /// </summary>
    IActionPolicy<TEntity> Define(string action, params string[] roles);
}
