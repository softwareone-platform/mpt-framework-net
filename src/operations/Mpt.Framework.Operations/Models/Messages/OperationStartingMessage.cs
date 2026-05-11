using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal class OperationStartingMessage<TOperation> : OperationMessage
{
    public required TOperation Data { get; init; }

    public override MessageGroup Group => MessageGroup.Main;
}
