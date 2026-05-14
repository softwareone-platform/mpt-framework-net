using FluentAssertions;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Tests.Configuration;

public class OperationProviderTests
{
    [Fact]
    public void IsEmpty_True_WhenNoOperationsRegistered()
    {
        var provider = new OperationProvider(new Dictionary<Type, OperationDescriptor>());

        provider.IsEmpty.Should().BeTrue();
        provider.GetDescriptors().Should().BeEmpty();
    }

    [Fact]
    public void GetDescriptors_ReturnsRegisteredItems()
    {
        var descriptor = MakeDescriptor("Sample");
        var provider = new OperationProvider(new Dictionary<Type, OperationDescriptor>
        {
            [typeof(SampleOperationContract)] = descriptor,
        });

        provider.IsEmpty.Should().BeFalse();
        provider.GetDescriptors().Should().ContainSingle().Which.Should().BeSameAs(descriptor);
    }

    private static OperationDescriptor MakeDescriptor(string name) => new()
    {
        Name = name,
        ModuleCode = "module",
        ImplementationType = typeof(object),
        SagaType = typeof(object),
        OperationType = typeof(SampleOperationContract),
        TaskType = typeof(object),
        GlobalPrefix = null,
    };

    private sealed class SampleOperationContract : IOperationContract
    {
        public string Id { get; set; } = string.Empty;
    }
}
