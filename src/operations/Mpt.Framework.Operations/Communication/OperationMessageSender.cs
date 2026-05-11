using MassTransit;
using Mpt.Framework.Operations.Configuration;
using Mpt.Framework.Operations.Models.Messages;
using System.Collections.Concurrent;

namespace Mpt.Framework.Operations.Communication;

internal class OperationMessageSender<TOperation> : IOperationMessageSender<TOperation>
    where TOperation : IOperationContract
{
    private readonly IOperationsBus _bus;
    private readonly OperationDescriptor _descriptor;
    private readonly OperationSettings _settings;
    private readonly string _transportType;
    private readonly ConcurrentDictionary<string, Task<ISendEndpoint>> _cache = [];

    public OperationMessageSender(IOperationsBus bus, OperationSettings settings, IOperationProvider operationProvider)
    {
        _bus = bus;

        if (!operationProvider.TryGetDescriptor<TOperation>(out var descriptor))
        {
            throw new InvalidOperationException($"Operation {typeof(TOperation).FullName} is not supported");
        }

        _descriptor = descriptor!;

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _transportType = _settings.Transport switch
        {
            OperationsTransport.InMemory => "queue",
            OperationsTransport.ServiceBus => "topic",
            _ => throw new InvalidOperationException($"Unsupported transport type {_settings.Transport}"),
        };
    }

    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellation)
        where TMessage : OperationMessage
    {
        var endpoint = await GetEndpontAsync(message.Group, cancellation);
        await endpoint.Send(message, ConfigureSend, cancellation);
    }

    public async Task SendManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellation)
        where TMessage : OperationMessage
    {
        foreach (var grp in messages.GroupBy(g => g.Group))
        {
            var endpoint = await GetEndpontAsync(grp.Key, cancellation);
            await endpoint.SendBatch(grp, ConfigureSend, cancellation);
        }
    }

    private async Task<ISendEndpoint> GetEndpontAsync(MessageGroup messageGroup, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var path = _settings.Transport switch
        {
            OperationsTransport.InMemory => _descriptor.GetQueueName(messageGroup),
            OperationsTransport.ServiceBus => _descriptor.TopicName,
            _ => throw new InvalidOperationException($"Unsupported transport type {_settings.Transport}"),
        };

        return await _cache.GetOrAdd($"{_transportType}:{path}", p => _bus.GetSendEndpoint(new Uri(p)));
    }

    private void ConfigureSend<TMessage>(SendContext<TMessage> context)
        where TMessage : OperationMessage
    {
        if (context.Message.Group == MessageGroup.Main || context.Message.Group == MessageGroup.Events)
        {
            context.SetSessionId(context.Message.OperationMetadata.Id.ToString());
        }

        context.Headers.Set(RoutingHelper.TargetHeaderName, _descriptor.GetTargetName(context.Message.Group));

        if (context.Message.Delay.HasValue)
            context.SetScheduledEnqueueTime(context.Message.Delay.Value);
    }
}
