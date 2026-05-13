using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mpt.Framework.Operation.Models;

namespace Mpt.Framework.Operation.Tests.EFCore;

public class OperationModelBuilderExtensionsTests
{
    [Fact]
    public void AddOperationEntity_ReturnsTheSameBuilder()
    {
        var builder = new ModelBuilder();

        var result = builder.AddOperationEntity();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddOperationEntity_AppliesTheSagaConfigurationToTheModel()
    {
        var builder = new ModelBuilder();

        builder.AddOperationEntity();

        var entity = builder.Model.FindEntityType(typeof(OperationSaga));
        entity.Should().NotBeNull("AddOperationEntity should register the OperationSaga entity on the model");
        entity!.GetTableName().Should().Be("Utils.Operations");
    }
}
