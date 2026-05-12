namespace Mpt.Framework.MessageHub;

internal interface IPlatformMessagePublisher
{
    Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken);
}
