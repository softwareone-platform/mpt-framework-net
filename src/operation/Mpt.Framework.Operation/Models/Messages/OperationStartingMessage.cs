using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class OperationStartingMessage<TOperation> : OperationMessage
{
    public required TOperation Data { get; init; }

    public override MessageGroup Group => MessageGroup.Main;
}
