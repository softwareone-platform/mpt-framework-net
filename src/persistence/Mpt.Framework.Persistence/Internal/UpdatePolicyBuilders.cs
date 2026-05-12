using FluentValidation;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>Top-level policy DSL implementation.</summary>
internal class UpdatePolicy<TEntity>(UpdatePolicyNode node) : IUpdatePolicy<TEntity>
{
    public IUpdatePolicy<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property, Action<IUpdatePolicyProperty<TEntity, TProperty>> configure)
        => MakeProperty(property, configure);

    public IUpdatePolicy<TEntity> Collection<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> property, Action<IUpdatePolicyProperty<TEntity, TProperty>> configure)
        => MakeProperty(property, configure);

    private UpdatePolicy<TEntity> MakeProperty<TProperty>(LambdaExpression lambda, Action<IUpdatePolicyProperty<TEntity, TProperty>> configure)
    {
        var child = node.Extend(lambda);
        var policyProperty = new UpdatePolicyProperty<TEntity, TProperty>(child);
        configure(policyProperty);

        return this;
    }
}

/// <summary>Per-property policy DSL implementation.</summary>
internal class UpdatePolicyProperty<TRoot, TProperty>(UpdatePolicyNode node) : IUpdatePolicyProperty<TRoot, TProperty>
{
    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Allow(params string[] roles)
        => Rule(roles, PolicyRuleAccess.Allowed);

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Forbid(params string[] roles)
        => Rule(roles, PolicyRuleAccess.Forbidden);

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Require(params string[] roles)
        => Rule(roles, PolicyRuleAccess.Required);

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Ignore(params string[] roles)
        => Rule(roles, PolicyRuleAccess.Ignored, r => r.IsCascade = true);

    public IUpdatePolicyProperty<TRoot, TChild> Property<TChild>(Expression<Func<TProperty, TChild>> property)
    {
        var child = node.Extend(property);
        return new UpdatePolicyProperty<TRoot, TChild>(child);
    }

    private UpdatePolicyRuleBuilder<TRoot, TProperty> Rule(string[] roles, PolicyRuleAccess access, Action<UpdatePolicyRule>? configure = null)
    {
        var rule = new UpdatePolicyRule
        {
            Access = access,
        };

        foreach (var role in roles)
        {
            rule.Roles.Add(role);
        }

        configure?.Invoke(rule);
        node.Rules.Add(rule);
        return new UpdatePolicyRuleBuilder<TRoot, TProperty>(rule);
    }
}

/// <summary>Chained rule builder implementation.</summary>
internal class UpdatePolicyRuleBuilder<TRoot, TProperty>(UpdatePolicyRule rule) : IUpdatePolicyRuleBuilder<TRoot, TProperty>
{
    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Hint(PropertyHints hints)
    {
        rule.Hints |= hints;
        return this;
    }

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Cascade()
    {
        rule.IsCascade = true;
        return this;
    }

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> If(Func<IUpdatePolicyData<TRoot, TProperty>, bool> condition)
    {
        rule.IfCondition = t =>
        {
            if (t is UpdatePolicyDataBag<TRoot> data)
                return condition(data.ToTarget<TProperty>());

            throw new InvalidOperationException($"Invalid data type {t.GetType().Name} — expected UpdatePolicyDataBag<{typeof(TRoot).Name}>");
        };
        return this;
    }

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> On(string action)
    {
        rule.Actions.Add(action);
        return this;
    }

    public IUpdatePolicyRuleBuilder<TRoot, TProperty> Validate(Action<InlineValidator<IUpdatePolicyData<TRoot, TProperty>>> action)
    {
        var validator = new InlineValidator<IUpdatePolicyData<TRoot, TProperty>>();
        validator.RuleFor(x => x).ChildRules(action);
        rule.ValidateCallback = t =>
        {
            if (t is UpdatePolicyDataBag<TRoot> data)
                return validator.Validate(data.ToTarget<TProperty>());

            throw new InvalidOperationException($"Invalid data type {t.GetType().Name} — expected UpdatePolicyDataBag<{typeof(TRoot).Name}>");
        };
        return this;
    }
}
