namespace Mpt.Framework.MessageHub;

[Flags]
public enum EventHints
{
    None = 0,

    /// <summary>
    /// Event is incomplete and most likely cannot be fully trusted.
    /// </summary>
    Incomplete = 1 << 0,

    /// <summary>
    /// Event represents an update that is not critical. Downstream services can choose
    /// to ignore this event completely.
    /// </summary>
    Silent = 1 << 1,

    /// <summary>
    /// Forces downstream sync logic to take into account entity revision.
    /// </summary>
    SoftSync = 1 << 2
}
