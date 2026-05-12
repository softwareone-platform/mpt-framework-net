using FluentValidation;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Chained-builder surface for refining an update-policy rule with action filter,
/// custom condition, validator, hint, and cascade flag.
/// </summary>
public interface IUpdatePolicyRuleBuilder<TRoot, TProperty>
{
    /// <summary>Restrict the rule to the specified entity action.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> On(EntityAction action) => On(action.ToString());

    /// <summary>Restrict the rule to the named action.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> On(string action);

    /// <summary>Restrict the rule to data that satisfies the predicate.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> If(Func<IUpdatePolicyData<TRoot, TProperty>, bool> condition);

    /// <summary>Attach a FluentValidation inline validator to the rule.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Validate(Action<InlineValidator<IUpdatePolicyData<TRoot, TProperty>>> action);

    /// <summary>Attach a hint that will be combined into the rule's hint bitmap.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Hint(PropertyHints hints);

    /// <summary>Mark the rule as cascading — its access decision becomes the default for any child properties.</summary>
    IUpdatePolicyRuleBuilder<TRoot, TProperty> Cascade();
}
