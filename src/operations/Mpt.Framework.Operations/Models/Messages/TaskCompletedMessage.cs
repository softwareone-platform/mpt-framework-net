using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal class TaskCompletedMessage : OperationMessage
{
    public required TaskMetadata TaskInfo { get; init; }

    public required TaskResult Result { get; init; }

    public override MessageGroup Group => MessageGroup.Events;
}
