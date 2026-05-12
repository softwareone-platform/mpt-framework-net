namespace Mpt.Framework;

/// <summary>
/// Marker for objects that carry a stable, string identity. Any framework component
/// (mapping, persistence, message-routing, etc.) that needs to match instances by id
/// keys on this interface.
/// </summary>
public interface IPlatformObject
{
    /// <summary>
    /// Gets or sets the stable identifier for this object.
    /// </summary>
    string Id { get; set; }
}
