namespace Mpt.Framework.MessageHub;

/// <summary>
/// Distinguishes an account-scoped principal from a user-scoped principal in
/// <see cref="EventMessagePrincipalAccess"/>.
/// </summary>
public enum EventMessagePrincipalType
{
    /// <summary>Principal represents an account / tenant / organisation.</summary>
    Account,
    /// <summary>Principal represents an individual user.</summary>
    User,
}

/// <summary>
/// One entry in a <see cref="PlatformEventPermissionsBuilder"/>'s principal list. Identifies
/// a principal authorised to see the event.
/// </summary>
/// <remarks>
/// The OSS <see cref="EventMessage"/> has no <c>Access</c> / <c>PrincipalAccess</c> fields,
/// so this data does not flow to the wire automatically. Consumers can read
/// <see cref="PlatformEventPermissionsBuilder.PrincipalAccess"/> from a
/// <c>MessageHubBuilder.OnMessagePublishing</c> hook and attach it to outgoing messages
/// however they need to.
/// </remarks>
public class EventMessagePrincipalAccess
{
    /// <summary>Stable principal identifier.</summary>
    public string Id { get; set; } = null!;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Optional display icon / avatar URL.</summary>
    public string? Icon { get; set; }

    /// <summary>Whether this principal is an account or an individual user.</summary>
    public EventMessagePrincipalType PrincipalType { get; set; }

    /// <summary>
    /// Consumer-defined account taxonomy. Upstream used a closed enum (<c>UserAccountType</c>);
    /// the OSS surface accepts any string so each consumer picks its own role/account labels.
    /// </summary>
    public string? AccountType { get; set; }
}
