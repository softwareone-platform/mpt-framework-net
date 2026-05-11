using MassTransit;
using Mpt.Framework.Operation.Communication;
using Mpt.Framework.Operation.Models;
using Mpt.Framework.Operation.Models.Messages;

namespace Mpt.Framework.Operation.Activities;

internal class OperationStartingActivity<TOperation, TSaga>(IOperationMessageSender<TOperation> sender) : IStateMachineActivity<TSaga, OperationStartingMessage<TOperation>>
    where TSaga : OperationSaga
{
    public void Accept(StateMachineVisitor visitor) { }

    public async Task Execute(BehaviorContext<TSaga, OperationStartingMessage<TOperation>> context, IBehavior<TSaga, OperationStartingMessage<TOperation>> next)
    {
        var message = new OperationPreparingMessage<TOperation> { OperationMetadata = context.Message.OperationMetadata, Data = context.Message.Data };
        await sender.SendAsync(message, context.CancellationToken);
        await next.Execute(context);
    }

    public async Task Faulted<TException>(BehaviorExceptionContext<TSaga, OperationStartingMessage<TOperation>, TException> context, IBehavior<TSaga, OperationStartingMessage<TOperation>> next) where TException : Exception
    {
        await next.Faulted(context);
    }

    public void Probe(ProbeContext context) { }
}
