using FluentValidation.Results;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Internal representation of a single rule attached to a node in the update-policy tree.
/// </summary>
internal class UpdatePolicyRule
{
    /// <summary>If non-empty, the rule applies only to these action names.</summary>
    public HashSet<string> Actions { get; } = [];

    /// <summary>The hints contributed to the property when this rule fires.</summary>
    public PropertyHints Hints { get; set; }

    /// <summary>When true, this rule's access decision also becomes the default for any child properties.</summary>
    public bool IsCascade { get; set; }

    /// <summary>The access decision the rule contributes.</summary>
    public PolicyRuleAccess Access { get; set; }

    /// <summary>If non-empty, the rule applies only when the caller's roles overlap this set.</summary>
    public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional custom predicate gating the rule.</summary>
    public Func<IUpdatePolicyData, bool>? IfCondition { get; set; }

    /// <summary>Optional FluentValidation callback evaluated when the rule fires.</summary>
    public Func<IUpdatePolicyData, ValidationResult>? ValidateCallback { get; set; }

    public bool IsApplicable<TEntity>(IUpdatePolicyData<TEntity> data)
    {
        if (Actions.Count > 0 && !Actions.Contains(data.Action))
            return false;

        // Roles match: empty role set means "all callers"; otherwise the caller's roles must overlap.
        if (Roles.Count > 0 && !data.Roles.Any(r => Roles.Contains(r)))
            return false;

        if (IfCondition != null && !IfCondition(data))
            return false;

        return true;
    }
}
