using MassTransit;
using Mpt.Framework.Operation.Communication;
using Mpt.Framework.Operation.Models.Messages;

namespace Mpt.Framework.Operation.Activities;

internal class TaskEventConsumer<TOperation>(IOperationMessageSender<TOperation> sender) : IConsumer<Batch<TaskCompletedMessage>>
{
    public async Task Consume(ConsumeContext<Batch<TaskCompletedMessage>> context)
    {
        foreach (var group in context.Message.GroupBy(msg => msg.Message.OperationMetadata.Id))
        {
            var info = group.First().Message.OperationMetadata;
            var results = group.ToLookup(k => k.Message.Result, v => v.Message.TaskInfo.Index);

            var summary = new BatchCompletedMessage
            {
                OperationMetadata = info,
                Succeded = [.. results[TaskResult.Success]],
                Failed = [.. results[TaskResult.Failure]],
            };

            await sender.SendAsync(summary, context.CancellationToken);
        }
    }
}
