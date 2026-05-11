using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal class OperationFailedMessage : OperationMessage
{
    public required OperationFailure Failure { get; init; }

    public override MessageGroup Group => MessageGroup.Main;
}
