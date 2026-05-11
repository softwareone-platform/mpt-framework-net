using Mpt.Framework.Operations.Models.Messages;

namespace Mpt.Framework.Operations.Communication;

internal interface IOperationMessageSender<TOperation>
{
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellation)
        where TMessage : OperationMessage;

    Task SendManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellation)
        where TMessage : OperationMessage;
}
