namespace Mpt.Framework.MessageHub;

/// <summary>
/// Forwards every <see cref="EventMessage"/> directly to the leaf
/// <see cref="IMessageHubPublisher"/>, awaiting the send inline. The default
/// <see cref="MessageHubPublishMode.Immediate"/> implementation.
/// </summary>
internal class ImmediatePlatformMessagePublisher(IMessageHubPublisher publishService)
    : IPlatformMessagePublisher
{
    public Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken)
        => publishService.PublishAsync(message.Message, cancellationToken);
}
