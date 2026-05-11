namespace Mpt.Framework.Operations;

public interface IOperationDispatcher
{
    Task<Guid> DispatchAsync<TContract>(TContract contract, CancellationToken cancellationToken)
        where TContract : IOperationContract
        => DispatchAsync(contract, null, cancellationToken);

    Task<Guid> DispatchAsync<TContract>(TContract contract, TimeSpan? delay, CancellationToken cancellationToken)
        where TContract : IOperationContract;

    Task DispatchManyAsync<TContract>(IEnumerable<(TContract Contract, TimeSpan? Delay)> items, CancellationToken cancellationToken)
        where TContract : IOperationContract;

    Task CancelAsync<TContract>(Guid operationId, CancellationToken cancellationToken)
        where TContract : IOperationContract;
}
