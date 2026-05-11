using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal class TaskCompletedMessage : OperationMessage
{
    public required TaskMetadata TaskInfo { get; init; }

    public required TaskResult Result { get; init; }

    public override MessageGroup Group => MessageGroup.Events;
}
