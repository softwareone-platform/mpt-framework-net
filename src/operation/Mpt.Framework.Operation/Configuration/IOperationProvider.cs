namespace Mpt.Framework.Operation.Configuration;

internal interface IOperationProvider
{
    bool IsEmpty { get; }

    bool TryGetDescriptor<TOperation>(out OperationDescriptor? descriptor)
        where TOperation : IOperationContract;

    IEnumerable<OperationDescriptor> GetDescriptors();
}
