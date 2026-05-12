namespace Mpt.Framework.MessageHub;

/// <summary>Writer side of the in-process channel used by <see cref="MessageHubPublishMode.Background"/> mode.</summary>
internal interface IPlatformEventChannelService
{
    /// <summary>Enqueues a message for the background publisher to drain.</summary>
    ValueTask AddMessage(TracedTransport<EventMessage> message, CancellationToken cancellationToken);
}
