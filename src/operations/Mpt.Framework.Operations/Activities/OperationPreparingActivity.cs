using MassTransit;
using Microsoft.Extensions.Logging;
using Mpt.Framework.Operations.Communication;
using Mpt.Framework.Operations.Configuration;
using Mpt.Framework.Operations.Models;
using Mpt.Framework.Operations.Models.Messages;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Mpt.Framework.Operations.Activities;

[SuppressMessage("Maintainability", "S2436", Justification = "Three params are needed")]
internal class OperationPreparingActivity<TOperation, TTask, TSaga>(IOperationMessageSender<TOperation> sender, IOperationProvider operationProvider,
    IOperation<TOperation, TTask> handler,
    ILogger<OperationPreparingActivity<TOperation, TTask, TSaga>> logger) : IStateMachineActivity<TSaga, OperationPreparingMessage<TOperation>>
    where TOperation : IOperationContract
    where TSaga : OperationSaga
{
    private const string TaskPublishingErrorMessage = "Operation failed while preparing tasks.";
    private const string NoTasksErrorMessage = "Operation must produce at least one task.";
    private const string ConditionCheckFailureMessage = "Operation failed while checking start condition.";

    private static readonly Action<ILogger, Exception?> _taskPublishingError = LoggerMessage.Define(LogLevel.Error,
        new EventId(1, nameof(ProduceTasksAsync)), TaskPublishingErrorMessage);

    private static readonly Action<ILogger, Exception?> _noTasksError = LoggerMessage.Define(LogLevel.Error,
        new EventId(2, nameof(ProduceTasksAsync)), NoTasksErrorMessage);

    private static readonly Action<ILogger, Exception?> _conditionCheckError = LoggerMessage.Define(LogLevel.Error,
        new EventId(3, nameof(CheckConditionAsync)), ConditionCheckFailureMessage);


    public void Accept(StateMachineVisitor visitor) { }

    public async Task Execute(BehaviorContext<TSaga, OperationPreparingMessage<TOperation>> context, IBehavior<TSaga, OperationPreparingMessage<TOperation>> next)
    {
        if (!context.Saga.StartCondition.IsSatisfied)
        {
            await CheckConditionAsync(context);
        }
        else
        {
            await ProduceTasksAsync(context);
            await next.Execute(context);
        }
    }

    private async Task CheckConditionAsync(BehaviorContext<TSaga, OperationPreparingMessage<TOperation>> context)
    {
        try
        {
            var startingContext = new OperationStartingContext
            {
                Attempt = context.Saga.StartCondition.Attempt,
                Operation = context.Message.Data,
                Metadata = context.Message.OperationMetadata
            };
            await handler.OnStartingAsync(startingContext, context.CancellationToken);

            context.Saga.StartCondition.IsSatisfied = !startingContext.Delay.HasValue;
            if (!context.Saga.StartCondition.IsSatisfied)
            {
                context.Saga.StartCondition.Attempt++;
            }

            context.Message.Delay = startingContext.Delay;

            await sender.SendAsync(context.Message, context.CancellationToken);
        }
        catch (Exception exc)
        {
            // If kill switch should be activated, rethrow to trigger the kill switch
            // procceed with standard path if it was the last attempt
            if (RetryHelper<TOperation>.ShouldRetry(operationProvider,
                descriptor => descriptor.Processing.Main, context, exc))
            {
                throw;
            }

            _conditionCheckError(logger, exc);
            await sender.SendAsync(new OperationFailedMessage
            {
                OperationMetadata = context.Message.OperationMetadata,
                Failure = new OperationFailure
                {
                    Type = OperationFailureType.ErrorCheckingCondition,
                    Message = ConditionCheckFailureMessage
                }
            }, context.CancellationToken);
        }
    }

    private async Task ProduceTasksAsync(BehaviorContext<TSaga, OperationPreparingMessage<TOperation>> context)
    {
        var total = 0;
        var index = 0;
        try
        {
            var preparingContext = new OperationPreparingContext
            {
                Operation = context.Message.Data,
                Metadata = context.Message.OperationMetadata
            };

            await foreach (var batch in handler.GetTasksAsync(preparingContext, context.CancellationToken).BatchAsync(100))
            {
                var toSend = new List<TaskStartingMessage<TTask>>(batch.Count);
                foreach (var item in batch)
                {
                    toSend.Add(new TaskStartingMessage<TTask>
                    {
                        OperationMetadata = context.Message.OperationMetadata,
                        Data = item,
                        TaskMetadata = new TaskMetadata
                        {
                            Id = Guid.NewGuid(),
                            Index = index++,
                        }
                    });
                }

                await sender.SendManyAsync(toSend, context.CancellationToken);
                total += batch.Count;
            }

            SetSagaOperationData(context);
            context.Saga.TaskStates = new OperationStateArray(total).Data;
        }
        catch (Exception exc)
        {
            // If kill switch should be activated, rethrow to trigger the kill switch
            // procceed with standard path if it was the last attempt
            if (RetryHelper<TOperation>.ShouldRetry(operationProvider,
                descriptor => descriptor.Processing.Main, context, exc))
            {
                throw;
            }

            // restore operation data if it was not set before, to allow failure handler to get it
            if (context.Saga.Data == null)
                SetSagaOperationData(context);

            _taskPublishingError(logger, exc);
            await sender.SendAsync(new OperationFailedMessage
            {
                OperationMetadata = context.Message.OperationMetadata,
                Failure = new OperationFailure
                {
                    Type = OperationFailureType.ErrorPreparingTasks,
                    Message = TaskPublishingErrorMessage
                }
            }, context.CancellationToken);
            return;
        }

        if (total == 0)
        {
            _noTasksError(logger, null);
            await sender.SendAsync(new OperationFailedMessage
            {
                OperationMetadata = context.Message.OperationMetadata,
                Failure = new OperationFailure
                {
                    Type = OperationFailureType.NoTasks,
                    Message = NoTasksErrorMessage
                }
            }, context.CancellationToken);
            return;
        }

        context.Saga.Statistics.Total = total;
    }

    public async Task Faulted<TException>(BehaviorExceptionContext<TSaga, OperationPreparingMessage<TOperation>, TException> context, IBehavior<TSaga, OperationPreparingMessage<TOperation>> next) where TException : Exception
    {
        await next.Faulted(context);
    }

    public void Probe(ProbeContext context) { }


    private sealed class OperationStartingContext : IOperationStartingContext<TOperation>
    {
        public int Attempt { get; set; }

        public TimeSpan? Delay { get; private set; }

        public required OperationMetadata Metadata { get; init; }

        public required TOperation Operation { get; init; }

        public void Postpone(TimeSpan delay)
        {
            Delay = delay;
        }
    }

    private static void SetSagaOperationData(BehaviorContext<TSaga, OperationPreparingMessage<TOperation>> context)
    {
        context.Saga.Data = JsonSerializer.SerializeToNode(context.Message.Data, OperationSerializerOptions.Default)!.AsObject();
    }

    private sealed class OperationPreparingContext : IOperationPreparingContext<TOperation>
    {
        public required OperationMetadata Metadata { get; init; }

        public required TOperation Operation { get; init; }
    }
}
