namespace Mpt.Framework.MessageHub;

public sealed class InputStream<TConsumer>(string providerKey, string name, StreamTypes sources)
    : InputStream(providerKey, name, sources)
{
    public override Type ConsumerType => typeof(TConsumer);
}
