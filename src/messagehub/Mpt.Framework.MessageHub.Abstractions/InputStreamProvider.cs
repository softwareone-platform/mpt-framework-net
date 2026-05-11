namespace Mpt.Framework.MessageHub;

/// <summary>
/// Base class for declaring the input streams a module consumes. Derive once per logical
/// "input source", override <see cref="GetInputStreams"/>, and register via
/// <c>MessageHubBuilder.ConfigureInput&lt;T&gt;()</c>.
/// </summary>
public abstract class InputStreamProvider : IInputStreamProvider
{
    /// <summary>
    /// Unique provider key used as a prefix for stream names.
    /// </summary>
    public virtual string Key { get; } = "gen";

    public abstract IEnumerable<InputStream> GetInputStreams();

    protected InputStream<TConsumer> DefineStream<TConsumer>(string name, StreamTypes sources, Action<InputStream>? configure = null)
    {
        var instance = new InputStream<TConsumer>(Key, name, sources);
        configure?.Invoke(instance);
        return instance;
    }
}
