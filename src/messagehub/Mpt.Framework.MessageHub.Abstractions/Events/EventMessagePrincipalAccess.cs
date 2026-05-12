namespace Mpt.Framework.MessageHub;

public enum EventMessagePrincipalType
{
    Account,
    User,
}

/// <summary>
/// One entry in a <see cref="PlatformEventPermissionsBuilder"/>'s principal list.
/// </summary>
public class EventMessagePrincipalAccess
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public EventMessagePrincipalType PrincipalType { get; set; }

    public string? AccountType { get; set; }
}
