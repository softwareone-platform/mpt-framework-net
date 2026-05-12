using Mpt.Framework.Delta;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Common surface available inside <see cref="IUpdatePolicyRuleBuilder{TRoot, TProperty}.If"/>
/// conditions and <see cref="IUpdatePolicyRuleBuilder{TRoot, TProperty}.Validate"/> callbacks.
/// </summary>
public interface IUpdatePolicyData
{
    /// <summary>The role names the caller carries for this action.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>The name of the action currently being evaluated (e.g. "Update").</summary>
    string Action { get; }

    /// <summary>Whether the property was present in the incoming delta (vs absent / unchanged).</summary>
    bool IsDefined { get; }
}

/// <summary>
/// <see cref="IUpdatePolicyData"/> with entity / delta access.
/// </summary>
public interface IUpdatePolicyData<TType> : IUpdatePolicyData
{
    /// <summary>The current (post-update) entity state.</summary>
    TType Entity { get; }

    /// <summary>The incoming delta describing the update.</summary>
    Delta<TType> Delta { get; }
}

/// <summary>
/// <see cref="IUpdatePolicyData{TType}"/> with the previous and incoming property values
/// for the specific property the rule is bound to.
/// </summary>
public interface IUpdatePolicyData<TType, out TProperty> : IUpdatePolicyData<TType>
{
    /// <summary>The original property value before the update.</summary>
    TProperty? Original { get; }

    /// <summary>The incoming property value from the delta.</summary>
    TProperty? Updated { get; }
}
