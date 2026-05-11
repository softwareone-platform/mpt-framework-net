namespace Mpt.Framework.MessageHub;

public interface IStreamProvider
{
    /// <summary>
    /// Unique provider key — used as a prefix in stream names so multiple providers in
    /// the same module don't collide.
    /// </summary>
    string Key { get; }

    IEnumerable<InputStream> GetInputStreams();
}

/// <summary>
/// Marker interface that distinguishes regular event consumers from specialized providers
/// (e.g. data-sync providers) that may want to register separately.
/// </summary>
public interface IInputStreamProvider : IStreamProvider { }
