using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Tests.Configuration;

public class OperationBuilderTests
{
    [Fact]
    public void Register_SameOperationTypeTwice_Throws()
    {
        var builder = NewBuilder();
        builder.Register<SampleOperation>("first.name");

        Action act = () => builder.Register<SampleOperation>("second.name");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration for operation * is already registered");
    }

    [Fact]
    public void Register_TypeThatDoesNotImplementGenericIOperation_Throws()
    {
        var builder = NewBuilder();

        Action act = () => builder.Register<NonGenericOperation>("some.name");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must implement IOperation<,> interface*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    [InlineData("invalid_name")]
    public void Register_InvalidName_ThrowsArgumentException(string name)
    {
        var builder = NewBuilder();

        Action act = () => builder.Register<SampleOperation>(name);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_ValidOperationInConsumeMode_RegistersHandlerInServices()
    {
        var services = new ServiceCollection();
        var builder = new OperationBuilder(services, "test", new OperationSettings { Mode = OperationMode.ConsumeAndDispatch });

        builder.Register<SampleOperation>("sample.op");

        services.Should().Contain(s => s.ServiceType == typeof(SampleOperation),
            "the concrete handler type should be registered in DI so MassTransit can resolve it per consumer scope");
    }

    [Fact]
    public void Register_ValidOperationInDispatchOnlyMode_DoesNotRegisterHandlerInServices()
    {
        var services = new ServiceCollection();
        var builder = new OperationBuilder(services, "test", new OperationSettings { Mode = OperationMode.Dispatch });

        builder.Register<SampleOperation>("sample.op");

        services.Should().NotContain(s => s.ServiceType == typeof(SampleOperation),
            "dispatch-only hosts never execute the handler, so the type should not be registered");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankModuleCode_Throws(string? moduleCode)
    {
        var act = () => new OperationBuilder(new ServiceCollection(), moduleCode!, new OperationSettings());

        act.Should().Throw<ArgumentException>().WithParameterName("moduleCode");
    }

    private static OperationBuilder NewBuilder() =>
        new(new ServiceCollection(), "test", new OperationSettings());

    private class SampleData : IOperationContract { }
    private class SampleTask { }

    private class SampleOperation : Operation<SampleData, SampleTask>
    {
        public override IAsyncEnumerable<SampleTask> GetTasksAsync(IOperationPreparingContext<SampleData> context, CancellationToken cancellationToken)
            => AsyncEnumerable.Empty<SampleTask>();

        public override Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<SampleTask> context, CancellationToken cancellationToken)
            => Task.FromResult(TaskResult.Success);
    }

    /// <summary>
    /// Implements only the non-generic <see cref="IOperation"/> marker — used to verify that
    /// <c>Register</c> rejects types that aren't valid operations.
    /// </summary>
    private class NonGenericOperation : IOperation { }
}
