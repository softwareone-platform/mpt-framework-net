using System.ComponentModel.DataAnnotations;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// Fluent builder for the list of principals authorised to see an event. The contained
/// principals do not auto-propagate to <see cref="EventMessage"/>; consumers that need
/// permissions on the wire read <see cref="PrincipalAccess"/> from a
/// <c>MessageHubBuilder.OnMessagePublishing</c> hook.
/// </summary>
public sealed class PlatformEventPermissionsBuilder
{
    private readonly List<EventMessagePrincipalAccess> _principalAccess = [];

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

    public PlatformEventPermissionsBuilder Clear()
    {
        _principalAccess.Clear();
        return this;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Helper for fluent API — must be an instance method to be reachable via builder chains.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Helper for fluent API — must be an instance method to be reachable via builder chains.")]
    public ValueTask CompletedTask() => ValueTask.CompletedTask;

    public IReadOnlyList<EventMessagePrincipalAccess> PrincipalAccess => _principalAccess;
}
