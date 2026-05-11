namespace Mpt.Framework.Operations;

public class OperationResult
{
    public OperationStatus Status { get; set; }

    public OperationFailure? Failure { get; set; }

    public OperationStatistics Statistics { get; set; } = null!;
}
