using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Per-property update-policy DSL. Attach an access rule (<see cref="Allow"/>,
/// <see cref="Forbid"/>, <see cref="Require"/>, <see cref="Ignore"/>) or descend into a
/// nested property via <see cref="Property{TChild}"/>.
/// </summary>
public interface IUpdatePolicyProperty<TRoot, TProperty>
{
    /// <summary>Allow the property to be set by callers holding any of <paramref name="roles"/> (or any caller if empty).</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Allow(params string[] roles);

    /// <summary>Forbid the property from being set by callers holding any of <paramref name="roles"/> (or any caller if empty).</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Forbid(params string[] roles);

    /// <summary>Require the property to be set by callers holding any of <paramref name="roles"/> (or any caller if empty).</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Require(params string[] roles);

    /// <summary>Silently ignore the property when set by callers holding any of <paramref name="roles"/> (or any caller if empty).</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Ignore(params string[] roles);

    /// <summary>Descend into a nested property of the current property and continue configuring it.</summary>
    IUpdatePolicyProperty<TRoot, TChild> Property<TChild>(Expression<Func<TProperty, TChild>> property);
}
