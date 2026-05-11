namespace Mpt.Framework.Operations;

public interface IOperation { }

public interface IOperation<TOperationContract, TTaskContract> : IOperation
    where TOperationContract : IOperationContract
{
    Task OnStartingAsync(IOperationStartingContext<TOperationContract> context, CancellationToken cancellationToken);

    IAsyncEnumerable<TTaskContract> GetTasksAsync(IOperationPreparingContext<TOperationContract> context, CancellationToken cancellationToken);

    Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<TTaskContract> context, CancellationToken cancellationToken);

    Task OnFinishedAsync(IOperationFinishedContext<TOperationContract> context, CancellationToken cancellationToken);

    bool AllMustSucceed { get; }
}

public abstract class Operation<TOperationContract, TTaskContract> : IOperation<TOperationContract, TTaskContract>
    where TOperationContract : IOperationContract
{
    public virtual Task OnStartingAsync(IOperationStartingContext<TOperationContract> context, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public abstract IAsyncEnumerable<TTaskContract> GetTasksAsync(IOperationPreparingContext<TOperationContract> context, CancellationToken cancellationToken);

    public abstract Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<TTaskContract> context, CancellationToken cancellationToken);

    public virtual Task OnFinishedAsync(IOperationFinishedContext<TOperationContract> context, CancellationToken cancellationToken) { return Task.CompletedTask; }

    public virtual bool AllMustSucceed => true;
}

public enum TaskResult
{
    Success,
    Failure
}
