using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Mpt.Framework.MessageHub;

[ExcludeFromCodeCoverage(Justification = "Thin Channel<T> writer wrapper with a logger fallback — the meaningful behaviour is the BackgroundService drain, which is exercised by the in-memory integration tests.")]
internal class PlatformEventChannelService(
    Channel<TracedTransport<EventMessage>> channel,
    ILogger<PlatformEventChannelService> logger)
    : IPlatformEventChannelService
{
    private readonly ChannelWriter<TracedTransport<EventMessage>> _writer = channel.Writer;
    private readonly ILogger<PlatformEventChannelService> _logger = logger;

    private static readonly Action<ILogger, string, Exception> _logErrorPublishingEvents
        = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(AddMessage)), "Error writing event(s) to channel: {EventKeys}");

    public async ValueTask AddMessage(TracedTransport<EventMessage> message, CancellationToken cancellationToken)
    {
        try
        {
            await _writer.WriteAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logErrorPublishingEvents(_logger, message.Message.Routing.ToPath(), ex);
        }
    }
}
