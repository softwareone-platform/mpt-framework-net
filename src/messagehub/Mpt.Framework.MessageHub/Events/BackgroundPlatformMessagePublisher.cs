namespace Mpt.Framework.MessageHub;

/// <summary>
/// Forwards every <see cref="EventMessage"/> into the in-process channel that
/// <see cref="PlatformEventBackgroundService"/> drains. The
/// <see cref="MessageHubPublishMode.Background"/> implementation.
/// </summary>
internal class BackgroundPlatformMessagePublisher(IPlatformEventChannelService channelService)
    : IPlatformMessagePublisher
{
    public async Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken)
        => await channelService.AddMessage(message, cancellationToken);
}
