using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace Mpt.Framework.MessageHub;

internal partial class PlatformEventBackgroundService(
    Channel<TracedTransport<EventMessage>> channel,
    IMessageHubPublisher publisher,
    ILogger<PlatformEventBackgroundService> logger)
    : BackgroundService
{
    private readonly ChannelReader<TracedTransport<EventMessage>> _reader = channel.Reader;
    private readonly ILogger<PlatformEventBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await _reader.WaitToReadAsync(stoppingToken))
        {
            await foreach (var message in _reader.ReadAllAsync(stoppingToken))
            {
                Activity? activity = null;

                if (message.ActivityContext is not null)
                {
                    activity = new Activity("MessageHub.Publish");
                    activity.SetParentId(message.ActivityContext.Value.TraceId, message.ActivityContext.Value.SpanId, ActivityTraceFlags.Recorded);
                    activity.Start();
                    Activity.Current = activity;
                }

                try
                {
                    await publisher.PublishAsync(message.Message, stoppingToken);
                }
                catch (Exception ex)
                {
                    var entity = message.Message.Objects.Find(entity => entity.Category == EventMessageObjectCategory.CurrentEntity);
                    LogSendEventError(eventKey: message.Message.Routing.Event, entityId: entity?.Id ?? "unknown", entityType: entity?.Type ?? "entity", ex);
                }
                finally
                {
                    activity?.Dispose();
                }
            }
        }
    }

    [LoggerMessage(LogLevel.Error, EventName = nameof(ExecuteAsync), Message = "Error sending {EventKey} event for {EntityType} {EntityId} to publisher.")]
    private partial void LogSendEventError(string eventKey, string entityId, string entityType, Exception ex);
}
