using FluentValidation.Results;
using Mpt.Framework.Delta;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Runtime evaluator that walks the policy tree and combines rule outputs into a
/// (Access, Hints, ValidationResult) decision for each property.
/// </summary>
internal class UpdatePolicyContext(
    UpdatePolicyNode node,
    IReadOnlyCollection<string> roles,
    string action,
    PolicyRuleAccess defaultAccess = PolicyRuleAccess.Forbidden,
    PropertyHints defaultHints = PropertyHints.None) : IUpdatePolicyContext
{
    public IReadOnlyCollection<string> Roles { get; } = roles;

    public string Action { get; } = action;

    public bool HasChildRules => node.Children.Count > 0;

    public PolicyRuleAccess DefaultAccess { get; private set; } = defaultAccess;

    public PropertyHints DefaultHints { get; private set; } = defaultHints;

    public (PolicyRuleAccess Access, PropertyHints Hints, ValidationResult ValidationResult) Execute<TEntity>(
        IReadOnlyCollection<string> roles,
        TEntity entity,
        Delta<TEntity> delta,
        object? original,
        object? updated,
        bool isDefined)
    {
        var data = new UpdatePolicyDataBag<TEntity>
        {
            Action = Action,
            Roles = roles,
            Entity = entity,
            Delta = delta,
            IsDefined = isDefined,
            Original = original,
            Updated = updated,
        };

        var result = DefaultAccess;
        var validationResult = new ValidationResult();
        var hints = PropertyHints.None;

        foreach (var rule in node.Rules)
        {
            if (!rule.IsApplicable(data))
                continue;

            if (rule.ValidateCallback != null)
            {
                validationResult.Errors.AddRange(rule.ValidateCallback(data).Errors);
            }

            if (rule.IsCascade)
            {
                DefaultAccess = rule.Access;
                DefaultHints = rule.Hints;
            }

            switch (rule.Access)
            {
                case PolicyRuleAccess.Allowed:
                    result |= PolicyRuleAccess.Allowed;
                    result &= ~PolicyRuleAccess.Forbidden;
                    break;
                case PolicyRuleAccess.Required:
                    result |= PolicyRuleAccess.Required;
                    result &= ~PolicyRuleAccess.Forbidden;
                    break;
                case PolicyRuleAccess.Forbidden:
                    result = PolicyRuleAccess.Forbidden;
                    break;
                case PolicyRuleAccess.Ignored:
                    result = PolicyRuleAccess.Ignored;
                    break;
                default:
                    throw new NotImplementedException($"Unknown access type {rule.Access}");
            }

            hints |= rule.Hints;
        }

        return (result, hints, validationResult);
    }

    public IUpdatePolicyContext GetChild(string propertyName)
    {
        if (!node.TryGetChild(propertyName, out var childNode))
        {
            childNode = UpdatePolicyNode.Empty;
        }

        return new UpdatePolicyContext(childNode!, Roles, Action, DefaultAccess, DefaultHints);
    }
}
