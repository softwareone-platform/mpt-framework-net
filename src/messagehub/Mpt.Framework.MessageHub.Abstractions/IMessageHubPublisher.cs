namespace Mpt.Framework.MessageHub;

/// <summary>
/// Publishes <see cref="EventMessage"/> instances onto the message hub. Implementations
/// route to the configured transport (in-memory or Azure Service Bus) and set the
/// per-message routing headers consumers' SQL filters rely on.
/// </summary>
public interface IMessageHubPublisher
{
    Task PublishAsync(EventMessage message, CancellationToken cancellationToken);
}
