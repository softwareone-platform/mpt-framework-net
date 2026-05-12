namespace Mpt.Framework.MessageHub;

/// <summary>
/// Publish-mode boundary between <see cref="PlatformEventEmitter"/> and the leaf
/// <see cref="IMessageHubPublisher"/>. Registered as either the immediate or background
/// flavour depending on <see cref="MessageHubSettings.PublishMode"/>.
/// </summary>
internal interface IPlatformMessagePublisher
{
    /// <summary>Publishes <paramref name="message"/> via the active publish strategy.</summary>
    Task PublishAsync(TracedTransport<EventMessage> message, CancellationToken cancellationToken);
}
