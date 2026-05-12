namespace Mpt.Framework.MessageHub;

internal class ImmediatePlatformMessagePublisher(IMessageHubPublisher publishService)
    : IPlatformMessagePublisher
{
    public Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken)
        => publishService.PublishAsync(message.Message, cancellationToken);
}
