using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Models.Messages;

internal abstract class OperationMessage
{
    public required OperationMetadata OperationMetadata { get; init; }

    public TimeSpan? Delay { get; set; }

    public abstract MessageGroup Group { get; }
}
