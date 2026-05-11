using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operations.Models.Messages;

namespace Mpt.Framework.Operations.Communication;

internal class OperationDispatcher(IServiceProvider serviceProvider) : IOperationDispatcher
{
    public async Task CancelAsync<TContract>(Guid operationId, CancellationToken cancellationToken)
        where TContract : IOperationContract
    {
        var sender = serviceProvider.GetRequiredService<IOperationMessageSender<TContract>>();
        await sender.SendAsync(new OperationCancelledMessage { OperationMetadata = new OperationMetadata { Id = operationId } }, cancellationToken);
    }

    public async Task<Guid> DispatchAsync<TContract>(TContract contract, TimeSpan? delay, CancellationToken cancellationToken)
        where TContract : IOperationContract
    {
        var sender = serviceProvider.GetRequiredService<IOperationMessageSender<TContract>>();
        var operationId = Guid.NewGuid();
        await sender.SendAsync(new OperationStartingMessage<TContract> { OperationMetadata = new() { Id = operationId }, Data = contract, Delay = delay }, cancellationToken);
        return operationId;
    }

    public async Task DispatchManyAsync<TContract>(IEnumerable<(TContract Contract, TimeSpan? Delay)> items, CancellationToken cancellationToken)
        where TContract : IOperationContract
    {
        var sender = serviceProvider.GetRequiredService<IOperationMessageSender<TContract>>();
        await sender.SendManyAsync(items.Select(s => new OperationStartingMessage<TContract> { OperationMetadata = new() { Id = Guid.NewGuid() }, Data = s.Contract, Delay = s.Delay }), cancellationToken);
    }
}
