using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Tests.EFCore;

public class OperationBuilderEfCoreExtensionsTests
{
    [Fact]
    public void UseSqlServerPersistence_WithNullBuilder_ThrowsArgumentNullException()
    {
        OperationBuilder builder = null!;

        var act = () => builder.UseSqlServerPersistence("Server=.;Database=ops;Integrated Security=True");

        act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UseSqlServerPersistence_WithBlankConnectionString_ThrowsArgumentException(string? connectionString)
    {
        var builder = MakeBuilder();

        var act = () => builder.UseSqlServerPersistence(connectionString!);

        act.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void UseSqlServerPersistence_WithValidConnectionString_ReplacesInMemoryPersistenceProvider()
    {
        var builder = MakeBuilder();
        var originalPersistence = builder.Persistence;

        var result = builder.UseSqlServerPersistence("Server=.;Database=ops;Integrated Security=True");

        result.Should().BeSameAs(builder);
        builder.Persistence.Should().NotBeSameAs(originalPersistence);
        builder.Persistence.GetType().Name.Should().Be("SqlServerPersistenceProvider");
    }

    private static OperationBuilder MakeBuilder()
        => new(new ServiceCollection(), "test-module", new OperationSettings());
}
