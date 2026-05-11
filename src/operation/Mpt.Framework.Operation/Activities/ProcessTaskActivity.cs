using MassTransit;
using Microsoft.Extensions.Logging;
using Mpt.Framework.Operation.Communication;
using Mpt.Framework.Operation.Configuration;
using Mpt.Framework.Operation.Models;
using Mpt.Framework.Operation.Models.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Activities;

[SuppressMessage("Maintainability", "S2436", Justification = "Three params are needed")]
internal class ProcessTaskActivity<TOperation, TTask, TSaga>(IOperationMessageSender<TOperation> sender, IOperationProvider operationProvider, IOperation<TOperation, TTask> handler, ILogger<ProcessTaskActivity<TOperation, TTask, TSaga>> logger)
    : IStateMachineActivity<TSaga, TaskStartingMessage<TTask>>
    where TOperation : IOperationContract
    where TSaga : OperationSaga
{
    private static readonly Action<ILogger, Exception?> _taskProcessingError = LoggerMessage.Define(LogLevel.Error,
        new EventId(1, nameof(Execute)), "An error occurred while processing task.");

    private static readonly Action<ILogger, Exception?> _taskFailedError = LoggerMessage.Define(LogLevel.Error,
        new EventId(2, nameof(Execute)), TaskFailedErrorMessage);

    private const string TaskFailedErrorMessage = "Task failed while configuration does not permit failure.";

    public void Accept(StateMachineVisitor visitor) { }

    public async Task Execute(BehaviorContext<TSaga, TaskStartingMessage<TTask>> context, IBehavior<TSaga, TaskStartingMessage<TTask>> next)
    {
        TaskResult result;
        try
        {
            var processingContext = new TaskProcessingContext
            {
                Metadata = context.Message.TaskMetadata,
                Task = context.Message.Data
            };

            result = await handler.ProcessTaskAsync(processingContext, context.CancellationToken);
        }
        catch (Exception exc)
        {
            // If kill switch should be activated, rethrow to trigger the kill switch
            // procceed with standard path if it was the last attempt
            if (RetryHelper<TOperation>.ShouldRetry(operationProvider,
                descriptor => descriptor.Processing.Tasks, context, exc))
            {
                throw;
            }

            _taskProcessingError(logger, exc);
            result = TaskResult.Failure;
        }

        if (handler.AllMustSucceed && result != TaskResult.Success)
        {
            _taskFailedError(logger, null);
            await sender.SendAsync(new OperationFailedMessage
            {
                OperationMetadata = context.Message.OperationMetadata,
                Failure = new OperationFailure
                {
                    Type = OperationFailureType.TaskFailedWhileNotAllowed,
                    Message = TaskFailedErrorMessage
                }

            }, context.CancellationToken);
            return;
        }

        var message = new TaskCompletedMessage
        {
            OperationMetadata = context.Message.OperationMetadata,
            TaskInfo = context.Message.TaskMetadata,
            Result = result,
        };

        await sender.SendAsync(message, context.CancellationToken);
        await next.Execute(context);
    }

    public async Task Faulted<TException>(BehaviorExceptionContext<TSaga, TaskStartingMessage<TTask>, TException> context, IBehavior<TSaga, TaskStartingMessage<TTask>> next) where TException : Exception
    {
        await next.Faulted(context);
    }

    public void Probe(ProbeContext context) { }


    private sealed class TaskProcessingContext : IProcessTaskContext<TTask>
    {
        public required TaskMetadata Metadata { get; init; }

        public required TTask Task { get; init; }
    }
}
