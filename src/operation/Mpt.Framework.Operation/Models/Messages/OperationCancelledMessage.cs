using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class OperationCancelledMessage : OperationMessage
{
    public override MessageGroup Group => MessageGroup.Main;
}
