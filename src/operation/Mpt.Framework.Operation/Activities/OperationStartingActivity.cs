using MassTransit;
using Mpt.Framework.Operation.Communication;
using Mpt.Framework.Operation.Models;
using Mpt.Framework.Operation.Models.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Activities;

[ExcludeFromCodeCoverage(Justification = "MassTransit state-machine activity — exercised end-to-end by the in-memory operation round-trip tests; the framework-internal boilerplate (Accept/Probe/Faulted) is not usefully unit-testable.")]
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
