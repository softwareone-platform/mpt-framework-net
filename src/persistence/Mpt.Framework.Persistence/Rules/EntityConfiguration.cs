using Mpt.Framework.Persistence.Internal;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Default <see cref="IEntityConfiguration{TEntity}"/> base class — derive and override
/// <see cref="ConfigureActions"/> and/or <see cref="ConfigureUpdate"/> to declare what
/// callers (by role name) may do to each property of <typeparamref name="TEntity"/>.
/// </summary>
public class EntityConfiguration<TEntity> : IEntityConfiguration<TEntity>
{
    private readonly Lazy<ActionPolicy<TEntity>> _actionPolicy;
    private readonly Lazy<UpdatePolicyNode> _updatePolicyNode;

    /// <summary>Initialises lazily-built policy state.</summary>
    public EntityConfiguration()
    {
        _actionPolicy = new Lazy<ActionPolicy<TEntity>>(() =>
        {
            var policy = new ActionPolicy<TEntity>();
            ConfigureActions(policy);
            return policy;
        });

        _updatePolicyNode = new Lazy<UpdatePolicyNode>(() =>
        {
            var node = new UpdatePolicyNode("root");
            var policy = new UpdatePolicy<TEntity>(node);
            ConfigureUpdate(policy);
            return node;
        });
    }

    /// <inheritdoc />
    public bool IsActionAllowed(string action, IReadOnlyCollection<string> roles)
        => _actionPolicy.Value.IsAllowed(action, roles);

    /// <summary>Ergonomic overload for callers holding a class reference.</summary>
    public bool IsActionAllowed(EntityAction action, IReadOnlyCollection<string> roles)
        => IsActionAllowed(action.ToString(), roles);

    /// <inheritdoc />
    public IUpdatePolicyContext GetUpdatePolicy(string action, IReadOnlyCollection<string> roles)
        => new UpdatePolicyContext(_updatePolicyNode.Value, roles, action);

    /// <summary>Ergonomic overload for callers holding a class reference.</summary>
    public IUpdatePolicyContext GetUpdatePolicy(EntityAction action, IReadOnlyCollection<string> roles)
        => GetUpdatePolicy(action.ToString(), roles);

    /// <summary>Override to declare which actions are permitted, and for which roles.</summary>
    protected virtual void ConfigureActions(IActionPolicy<TEntity> policy) { }

    /// <summary>Override to declare per-property update rules.</summary>
    protected virtual void ConfigureUpdate(IUpdatePolicy<TEntity> policy) { }
}
