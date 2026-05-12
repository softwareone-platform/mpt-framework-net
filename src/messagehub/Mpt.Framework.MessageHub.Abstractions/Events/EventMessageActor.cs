namespace Mpt.Framework.MessageHub;

/// <summary>
/// Minimal description of the principal that triggered an event. Stamped on
/// outgoing <see cref="EventMessage"/> instances by <see cref="IPlatformEventEmitter"/>
/// when an <see cref="IPlatformEventActorProducer"/> is registered.
/// </summary>
/// <remarks>
/// Upstream's <c>EventMessageActor</c> carried a nested <c>EventMessageActorAccount</c>
/// with a <c>UserAccountType</c> enum (Client / Vendor / Operations). The OSS surface
/// flattens this — consumers who need account information can stash it in <see cref="EventMessageObject.Data"/>
/// themselves via a custom <see cref="IPlatformEventActorProducer"/>.
/// </remarks>
public class EventMessageActor
{
    /// <summary>Stable principal identifier (user id, service principal id, etc.).</summary>
    public string Id { get; set; } = null!;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Optional display icon / avatar URL.</summary>
    public string? Icon { get; set; }
}
