namespace Mpt.Framework.Operation;

public interface IOperationFinishedContext<out TOperationData> : IOperationContext<TOperationData>
{
    /// <summary>
    /// Operation result.
    /// </summary>
    public OperationResult Result { get; }
}
