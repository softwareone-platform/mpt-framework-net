using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class OperationPreparingMessage<TOperation> : OperationMessage
{
    public required TOperation Data { get; init; }

    public override MessageGroup Group => MessageGroup.Main;
}
