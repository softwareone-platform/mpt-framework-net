using Mpt.Framework.Operation.Models.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Communication;

[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "Phantom type parameter used for DI registration / resolution keying per operation type.")]
internal interface IOperationMessageSender<TOperation>
{
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellation)
        where TMessage : OperationMessage;

    Task SendManyAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellation)
        where TMessage : OperationMessage;
}
