using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic marker for entity-configuration implementations — exists so DI can
/// locate them by interface during assembly scanning.
/// </summary>
public interface IEntityConfiguration;

/// <summary>
/// Per-entity declarative configuration: defines which actions are permitted for
/// which roles (<see cref="IActionPolicy{TEntity}"/>) and which property-level update
/// rules apply (<see cref="IUpdatePolicy{TEntity}"/>).
/// </summary>
/// <typeparam name="TEntity">The entity being configured. Used as a phantom type for DI keying — implementations are registered and resolved by <c>IEntityConfiguration&lt;T&gt;</c>.</typeparam>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "Phantom type parameter used for DI registration / resolution keying.")]
public interface IEntityConfiguration<in TEntity> : IEntityConfiguration
{
    /// <summary>Returns true if the action is permitted for any of <paramref name="roles"/>.</summary>
    [ExcludeFromCodeCoverage(Justification = "Default-interface convenience overload stringifying EntityAction. EntityConfiguration<T> reimplements it as a class-direct method for callers holding a concrete reference, so this default body is unreachable for that base class.")]
    bool IsActionAllowed(EntityAction action, IReadOnlyCollection<string> roles)
        => IsActionAllowed(action.ToString(), roles);

    /// <summary>Returns true if the named action is permitted for any of <paramref name="roles"/>.</summary>
    bool IsActionAllowed(string action, IReadOnlyCollection<string> roles);

    /// <summary>Returns the runtime update-policy context for the supplied action / roles.</summary>
    [ExcludeFromCodeCoverage(Justification = "Default-interface convenience overload stringifying EntityAction. EntityConfiguration<T> reimplements it as a class-direct method.")]
    IUpdatePolicyContext GetUpdatePolicy(EntityAction action, IReadOnlyCollection<string> roles)
        => GetUpdatePolicy(action.ToString(), roles);

    /// <summary>Returns the runtime update-policy context for the named action / roles.</summary>
    IUpdatePolicyContext GetUpdatePolicy(string action, IReadOnlyCollection<string> roles);
}
