using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class OperationFailedMessage : OperationMessage
{
    public required OperationFailure Failure { get; init; }

    public override MessageGroup Group => MessageGroup.Main;
}
