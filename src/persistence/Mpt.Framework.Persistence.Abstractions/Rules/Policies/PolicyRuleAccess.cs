namespace Mpt.Framework.Persistence;

/// <summary>
/// Access decision produced by an update-policy rule.
/// </summary>
[Flags]
public enum PolicyRuleAccess
{
    /// <summary>No access decision.</summary>
    None = 0,

    /// <summary>The property may be set.</summary>
    Allowed = 1 << 0,

    /// <summary>The property must be set; missing values produce a validation error.</summary>
    Required = 1 << 1,

    /// <summary>The property must not be set; incoming values produce a validation error.</summary>
    Forbidden = 1 << 2,

    /// <summary>The property is silently ignored — no error, no apply.</summary>
    Ignored = 1 << 3,
}
