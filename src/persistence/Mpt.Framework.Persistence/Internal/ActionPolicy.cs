namespace Mpt.Framework.Persistence.Internal;

internal class ActionPolicy<TEntity> : IActionPolicy<TEntity>
{
    private readonly Dictionary<string, HashSet<string>> _permissions =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsAllowed(string action, IReadOnlyCollection<string> roles)
    {
        if (!_permissions.TryGetValue(action, out var allowedRoles))
            return false;

        // Empty allowed-set means "all callers" — every action explicitly registered with no
        // role restriction is permitted regardless of the caller's roles.
        if (allowedRoles.Count == 0)
            return true;

        return roles.Any(allowedRoles.Contains);
    }

    public IActionPolicy<TEntity> Define(string action, params string[] roles)
    {
        if (_permissions.ContainsKey(action))
            throw new InvalidOperationException($"Action {action} is already defined");

        _permissions.Add(action, new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase));
        return this;
    }
}
