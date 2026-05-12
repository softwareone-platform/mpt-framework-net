namespace Mpt.Framework.MessageHub;

internal interface IPlatformEventChannelService
{
    ValueTask AddMessage(TracedTransport<EventMessage> message, CancellationToken cancellationToken);
}
