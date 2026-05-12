using FluentValidation.Results;
using Mpt.Framework.Delta;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Runtime view of a node in the update-policy tree. Walks property-by-property and
/// produces a (Access, Hints, ValidationResult) decision for each property the
/// engine encounters.
/// </summary>
public interface IUpdatePolicyContext
{
    /// <summary>Walks down to the named child property of this node.</summary>
    IUpdatePolicyContext GetChild(string propertyName);

    /// <summary>
    /// Evaluates the rules registered at this node against the supplied data and returns
    /// the combined access decision, hints, and validator output.
    /// </summary>
    (PolicyRuleAccess Access, PropertyHints Hints, ValidationResult ValidationResult) Execute<TEntity>(
        IReadOnlyCollection<string> roles,
        TEntity entity,
        Delta<TEntity> delta,
        object? original,
        object? updated,
        bool isDefined);

    /// <summary>Whether this node has any child rules.</summary>
    bool HasChildRules { get; }
}
