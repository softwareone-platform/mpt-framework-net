namespace Mpt.Framework;

/// <summary>
/// Implemented by entities that carry a monotonically-increasing revision number.
/// Framework components that need optimistic concurrency or "what changed" tracking
/// read and write this property.
/// </summary>
public interface IRevisable
{
    /// <summary>
    /// Gets or sets the current revision of this entity.
    /// </summary>
    int Revision { get; set; }
}
