using MassTransit;
using Microsoft.Extensions.Logging;

namespace Mpt.Framework.MessageHub.Internal;

internal partial class MessageHubPublisher(
    IMessageHubBus bus,
    MessageHubBuilder builder,
    ILogger<MessageHubPublisher> logger) : IMessageHubPublisher
{
    public async Task PublishAsync(EventMessage message, CancellationToken cancellationToken)
    {
        builder.OnMessagePublishing?.Invoke(message);

        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await bus.Publish(message, context =>
            {
                if (message.Routing.Delay.HasValue)
                    context.SetScheduledEnqueueTime(message.Routing.Delay.Value);

                if (!string.IsNullOrEmpty(message.SessionId))
                    context.SetSessionId(message.SessionId);

                if (!string.IsNullOrEmpty(message.PartitionKey))
                    context.SetPartitionKey(message.PartitionKey);

                foreach (var (key, value) in StreamRoutingHelper.GetOutputAttributes(message))
                    context.Headers.Set(key, value);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            var entity = message.Objects.Find(o => o.Category == EventMessageObjectCategory.CurrentEntity);
            LogSendEventError(
                eventKey: message.Routing.Event,
                entityId: entity?.Id ?? "unknown",
                entityType: entity?.Type ?? "entity",
                ex);
        }
    }

    [LoggerMessage(LogLevel.Error, EventName = nameof(PublishAsync),
        Message = "Error publishing {EventKey} event for {EntityType} {EntityId} to MessageHub.")]
    private partial void LogSendEventError(string eventKey, string entityId, string entityType, Exception ex);
}
