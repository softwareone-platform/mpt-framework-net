namespace Mpt.Framework.Persistence;

/// <summary>
/// Hints attached to update-policy rules that influence how the persistence engine
/// applies an incoming value.
/// </summary>
[Flags]
public enum PropertyHints
{
    /// <summary>No hints.</summary>
    None = 0,

    /// <summary>
    /// Trust the incoming platform-entity reference as-is rather than re-loading it
    /// through the configured query service / repository. Use sparingly — appropriate
    /// when the caller has already validated the reference against the persistence layer.
    /// </summary>
    TrustReference = 1 << 0,
}
