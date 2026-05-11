using Mpt.Framework.Operations.Models;
using Mpt.Framework.Operations.Utility;

namespace Mpt.Framework.Operations.Tests.Reflection;

public class SagaTypeBuilderTests
{
    [Fact]
    public void MakeSagaType_ShouldReturnSameType_ForSameOperation()
    {
        // Arrange
        Type operationType = typeof(SampleOperation);

        // Act
        Type sagaType1 = OperationSagaTypeBuilder.MakeSagaType(operationType, "abc");
        Type sagaType2 = OperationSagaTypeBuilder.MakeSagaType(operationType, "abc");

        // Assert
        Assert.Equal(sagaType1, sagaType2);
    }

    [Fact]
    public void MakeSagaType_ShouldCreateType_WithExpectedName()
    {
        // Arrange
        Type operationType = typeof(SampleOperation);
        string expectedTypeName = $"{operationType.Name}Saga";

        // Act
        Type sagaType = OperationSagaTypeBuilder.MakeSagaType(operationType, "abc");

        // Assert
        Assert.StartsWith(expectedTypeName, sagaType.Name);
    }

    [Fact]
    public void MakeSagaType_ShouldCreateType_ThatInheritsFromOperationSaga()
    {
        // Arrange
        Type operationType = typeof(SampleOperation);

        // Act
        Type sagaType = OperationSagaTypeBuilder.MakeSagaType(operationType, "abc");

        // Assert
        Assert.True(typeof(OperationSaga).IsAssignableFrom(sagaType));
    }

    [Fact]
    public void MakeSagaType_InstanceTypeProperty_ShouldHaveGivenValue()
    {
        // Arrange
        Type operationType = typeof(SampleOperation);

        // Act
        Type sagaType = OperationSagaTypeBuilder.MakeSagaType(operationType, "abc");
        var instance = Activator.CreateInstance(sagaType) as OperationSaga;

        // Assert
        Assert.Equal("abc", instance!.Type);
    }

    private class SampleOperation
    {
    }
}
