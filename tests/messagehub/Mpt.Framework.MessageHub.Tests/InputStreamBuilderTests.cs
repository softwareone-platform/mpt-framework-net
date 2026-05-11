using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub.Internal;
using NSubstitute;

namespace Mpt.Framework.MessageHub.Tests;

public class InputStreamBuilderTests
{
    private readonly MessageHubBuilder _builder;
    private readonly IStreamProvider _provider1;
    private readonly IStreamProvider _provider2;
    private readonly InputStream _stream1;
    private readonly InputStream _stream2;

    public InputStreamBuilderTests()
    {
        _builder = new MessageHubBuilder(new ServiceCollection(), "test-module");
        _builder.Settings.ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Key;SharedAccessKey=fake";

        _stream1 = new TestStream<TestConsumer>("stream1", StreamTypes.Events);
        _stream2 = new TestStream<TestConsumer2>("stream2", StreamTypes.Sync);

        _provider1 = Substitute.For<IStreamProvider>();
        _provider1.Key.Returns("test");
        _provider1.GetInputStreams().Returns([_stream1]);

        _provider2 = Substitute.For<IStreamProvider>();
        _provider2.Key.Returns("test2");
        _provider2.GetInputStreams().Returns([_stream2]);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        var act = () => new InputStreamBuilder("module", _builder, _provider1, _provider2);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyModuleName_Throws(string? name)
    {
        var act = () => new InputStreamBuilder(name!, _builder, _provider1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Stream name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_DuplicateStreamNames_Throws()
    {
        var duplicateProvider = Substitute.For<IStreamProvider>();
        duplicateProvider.Key.Returns("test");
        duplicateProvider.GetInputStreams().Returns([_stream1]);

        var act = () => new InputStreamBuilder("module", _builder, _provider1, duplicateProvider);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Input stream providers must have unique stream names*");
    }

    [Fact]
    public void Constructor_NullProvidersInArray_FiltersOutNulls()
    {
        var act = () => new InputStreamBuilder("module", _builder, _provider1, null, _provider2);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterInputStreamTypes_AddsEachConsumerType()
    {
        var builder = new InputStreamBuilder("module", _builder, _provider1, _provider2);
        var configurator = Substitute.For<IBusRegistrationConfigurator>();

        var act = () => builder.RegisterInputStreamTypes(configurator);

        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureInputStreams_UnsupportedConfigurator_Throws()
    {
        var builder = new InputStreamBuilder("module", _builder, _provider1);
        var context = Substitute.For<IBusRegistrationContext>();
        var configurator = Substitute.For<IBusFactoryConfigurator>();

        var act = () => builder.ConfigureInputStreams(context, configurator);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported bus factory configurator type*");
    }

    private class TestStream<TConsumer>(string name, StreamTypes sources) : InputStream("test", name, sources)
    {
        public override Type ConsumerType => typeof(TConsumer);
    }

    // These are public because MassTransit's AddConsumer internally generates Castle DynamicProxy
    // instances over interfaces parameterized by the consumer type. Private nested classes from a
    // strong-named test assembly aren't accessible to DynamicProxyGenAssembly2.
    public class TestConsumer : IConsumer<EventMessage>
    {
        public Task Consume(ConsumeContext<EventMessage> context) => Task.CompletedTask;
    }

    public class TestConsumer2 : IConsumer<EventMessage>
    {
        public Task Consume(ConsumeContext<EventMessage> context) => Task.CompletedTask;
    }
}
