namespace Mpt.Framework.MessageHub;

internal class BackgroundPlatformMessagePublisher(IPlatformEventChannelService channelService)
    : IPlatformMessagePublisher
{
    public async Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken)
        => await channelService.AddMessage(message, cancellationToken);
}
