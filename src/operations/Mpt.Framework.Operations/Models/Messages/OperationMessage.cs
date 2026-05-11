using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Models.Messages;

internal abstract class OperationMessage
{
    public required OperationMetadata OperationMetadata { get; init; }

    public TimeSpan? Delay { get; set; }

    public abstract MessageGroup Group { get; }
}
