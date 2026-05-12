using System.ComponentModel.DataAnnotations;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// Fluent builder for the list of principals authorised to see an event. Passed to
/// <c>Generic*Event&lt;TEntity&gt;</c> constructors to preserve upstream call-site
/// shape; the contained principals do not auto-propagate to the wire — OSS
/// <see cref="EventMessage"/> has no <c>Access</c> / <c>PrincipalAccess</c> fields.
/// Consumers that need to attach permissions to outgoing messages can read
/// <see cref="PrincipalAccess"/> from a <c>MessageHubBuilder.OnMessagePublishing</c>
/// hook and add their own <see cref="EventMessageObject"/>.
/// </summary>
/// <remarks>
/// Upstream's <c>Set(UserAccountType, …)</c> overload was already <c>[Obsolete]</c> and
/// depended on business taxonomy types (<c>UserAccountType</c>, <c>EventMessageAccountTypeAccess</c>,
/// <c>AccountPermissionType</c>). It is not ported. The string-typed
/// <see cref="AddAccountPrincipalAccess"/> overload is the supported entry point.
/// </remarks>
public sealed class PlatformEventPermissionsBuilder
{
    private readonly List<EventMessagePrincipalAccess> _principalAccess = [];

    /// <summary>
    /// Adds an account-scoped principal. <paramref name="accountType"/> is a free-form
    /// string (no fixed enum); each consumer picks its own taxonomy.
    /// </summary>
    public PlatformEventPermissionsBuilder AddAccountPrincipalAccess(
        string id,
        string accountType,
        string? name = null,
        string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException("Account ID cannot be null or whitespace.");
        }

        _principalAccess.Add(new EventMessagePrincipalAccess
        {
            Id = id,
            Name = name,
            Icon = icon,
            PrincipalType = EventMessagePrincipalType.Account,
            AccountType = accountType,
        });

        return this;
    }

    /// <summary>Adds a user-scoped principal.</summary>
    public PlatformEventPermissionsBuilder AddUserPrincipalAccess(string id, string? name = null, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException("User ID cannot be null or whitespace.");
        }

        _principalAccess.Add(new EventMessagePrincipalAccess
        {
            Id = id,
            Name = name,
            Icon = icon,
            PrincipalType = EventMessagePrincipalType.User,
        });

        return this;
    }

    /// <summary>Clears the principal list.</summary>
    public PlatformEventPermissionsBuilder Clear()
    {
        _principalAccess.Clear();
        return this;
    }

    /// <summary>Awaitable no-op used by fluent permission-builder pipelines that return <see cref="ValueTask"/>.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Helper property for fluent API.")]
    public ValueTask CompletedTask() => ValueTask.CompletedTask;

    /// <summary>The principals registered so far.</summary>
    public IReadOnlyList<EventMessagePrincipalAccess> PrincipalAccess => _principalAccess;
}
