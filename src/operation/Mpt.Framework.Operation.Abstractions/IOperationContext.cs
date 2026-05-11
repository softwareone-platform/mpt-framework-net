namespace Mpt.Framework.Operation;

public interface IOperationContext<out TOperationData>
{
    public OperationMetadata Metadata { get; }

    /// <summary>
    /// Operation data.
    /// </summary>
    TOperationData Operation { get; }
}
