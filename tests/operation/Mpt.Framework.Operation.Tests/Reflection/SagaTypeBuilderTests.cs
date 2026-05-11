using FluentAssertions;
using Mpt.Framework.Operation.Models;
using Mpt.Framework.Operation.Utility;

namespace Mpt.Framework.Operation.Tests.Reflection;

// OperationSagaTypeBuilder caches by operation Type, so each test below uses its own private
// nested class. Sharing a type across tests would leak the first test's name into the others.
public class SagaTypeBuilderTests
{
    [Fact]
    public void MakeSagaType_SameOperationCalledTwice_ReturnsCachedType()
    {
        var first = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForCacheTest), "abc");
        var second = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForCacheTest), "abc");

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void MakeSagaType_DifferentOperations_ReturnDifferentTypes()
    {
        var a = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForDifferentTestA), "a");
        var b = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForDifferentTestB), "b");

        a.Should().NotBeSameAs(b);
    }

    [Fact]
    public void MakeSagaType_CreatedTypeHasNamePrefixedByOperationName()
    {
        var sagaType = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForNameTest), "any");

        sagaType.Name.Should().StartWith($"{nameof(OpForNameTest)}Saga");
    }

    [Fact]
    public void MakeSagaType_CreatedTypeInheritsFromOperationSaga()
    {
        var sagaType = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForInheritsTest), "any");

        typeof(OperationSaga).IsAssignableFrom(sagaType).Should().BeTrue();
    }

    [Fact]
    public void MakeSagaType_InstanceFromParameterlessCtor_HasTypeSetToDiscriminator()
    {
        var sagaType = OperationSagaTypeBuilder.MakeSagaType(typeof(OpForTypePropTest), "abc");

        var instance = (OperationSaga)Activator.CreateInstance(sagaType)!;

        instance.Type.Should().Be("abc");
    }

    private class OpForCacheTest { }
    private class OpForDifferentTestA { }
    private class OpForDifferentTestB { }
    private class OpForNameTest { }
    private class OpForInheritsTest { }
    private class OpForTypePropTest { }
}
