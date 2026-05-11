using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal class TaskStartingMessage<TTask> : OperationMessage
{
    public required TaskMetadata TaskMetadata { get; init; }

    public required TTask Data { get; init; }

    public override MessageGroup Group => MessageGroup.Tasks;
}
