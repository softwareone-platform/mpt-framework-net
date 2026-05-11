using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class BatchCompletedMessage : OperationMessage
{
    public List<int> Succeded { get; set; } = [];

    public List<int> Failed { get; set; } = [];

    public override MessageGroup Group => MessageGroup.Main;
}
