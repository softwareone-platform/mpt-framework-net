using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal class OperationCancelledMessage : OperationMessage
{
    public override MessageGroup Group => MessageGroup.Main;
}
