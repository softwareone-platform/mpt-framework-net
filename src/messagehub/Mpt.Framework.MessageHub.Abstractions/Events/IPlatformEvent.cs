namespace Mpt.Framework.MessageHub;

/// <summary>
/// Marker for an event-authoring object that can serialise itself to a wire
/// <see cref="EventMessage"/>. Implementations are typically subclasses of
/// <see cref="PlatformEvent"/>.
/// </summary>
public interface IPlatformEvent
{
    /// <summary>Builds the wire-level <see cref="EventMessage"/> for publication.</summary>
    EventMessage MakeMessage();
}
