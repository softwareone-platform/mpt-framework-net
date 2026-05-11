using MassTransit;
using Mpt.Framework.Operations.Models;
using System.Text.Json;

namespace Mpt.Framework.Operations.Activities;

internal class OperationFinishedActivity<TOperation, TTask, TSaga>(IOperation<TOperation, TTask> handler) : IStateMachineActivity<TSaga>
    where TOperation : IOperationContract
    where TSaga : OperationSaga
{
    public void Accept(StateMachineVisitor visitor) { }

    public async Task Execute(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteInternal(context);
        await next.Execute(context);
    }

    public async Task Execute<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next) where T : class
    {
        await ExecuteInternal(context);
        await next.Execute(context);
    }

    private async Task ExecuteInternal(BehaviorContext<TSaga> context)
    {
        var saga = context.Saga;

        saga.Timestamps.Finished = DateTimeOffset.UtcNow;
        saga.Statistics.Cancelled = context.Saga.Statistics.Total - context.Saga.Statistics.Failed - context.Saga.Statistics.Succeded;

        var data = saga.Data.Deserialize<TOperation>(OperationSerializerOptions.Default)!;

        var status = saga.Status switch
        {
            "Failed" => OperationStatus.Failed,
            "Cancelled" => OperationStatus.Cancelled,
            _ => OperationStatus.Succeeded
        };

        var finishContext = new OperationFinishedContext
        {
            Operation = data,
            Result = new OperationResult()
            {
                Status = status,
                Failure = saga.Failure,
                Statistics = new OperationStatistics
                {
                    Total = saga.Statistics.Total,
                    Succeeded = saga.Statistics.Succeded,
                    Cancelled = saga.Statistics.Cancelled,
                    Failed = saga.Statistics.Failed
                }
            },
            Metadata = new OperationMetadata
            {
                Id = saga.CorrelationId,
            },
        };

        await handler.OnFinishedAsync(finishContext, context.CancellationToken);
    }

    public async Task Faulted<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next) where TException : Exception
        => await next.Faulted(context);

    public async Task Faulted<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
        => await next.Faulted(context);

    public void Probe(ProbeContext context) { }

    internal class OperationFinishedContext : IOperationFinishedContext<TOperation>
    {
        public required TOperation Operation { get; init; }

        public required OperationResult Result { get; init; }

        public required OperationMetadata Metadata { get; init; }
    }
}
