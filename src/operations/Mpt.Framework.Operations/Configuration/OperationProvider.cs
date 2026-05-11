namespace Mpt.Framework.Operations.Configuration;

internal class OperationProvider(IReadOnlyDictionary<Type, OperationDescriptor> items) : IOperationProvider
{
    public bool IsEmpty => items.Count == 0;

    public IEnumerable<OperationDescriptor> GetDescriptors() => items.Values;

    public bool TryGetDescriptor<TOperation>(out OperationDescriptor? descriptor) where TOperation : IOperationContract
        => items.TryGetValue(typeof(TOperation), out descriptor);
}
