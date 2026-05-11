using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class InputStreamProviderTests
{
    [Fact]
    public void DefineStream_ReturnsInputStreamForConsumerType()
    {
        var provider = new TestProvider();

        var stream = provider.DefineTestStream<TestConsumer>("orders", StreamTypes.Events);

        stream.Should().BeOfType<InputStream<TestConsumer>>();
        stream.Provider.Should().Be("test");
        stream.Name.Should().Be("orders");
        stream.Sources.Should().Be(StreamTypes.Events);
        stream.ConsumerType.Should().Be(typeof(TestConsumer));
    }

    [Fact]
    public void DefineStream_InvokesConfigureCallbackOnTheStream()
    {
        var provider = new TestProvider();

        var stream = provider.DefineTestStream<TestConsumer>("orders", StreamTypes.Events, s =>
        {
            s.Filter.Modules = ["accounts"];
            s.Settings.PrefetchCount = 64;
        });

        stream.Filter.Modules.Should().BeEquivalentTo(["accounts"]);
        stream.Settings.PrefetchCount.Should().Be(64);
    }

    [Fact]
    public void InputStream_Ctor_RejectsStreamTypesNone()
    {
        var act = () => new InputStream<TestConsumer>("test", "orders", StreamTypes.None);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one source must be specified*");
    }

    [Fact]
    public void InputStream_GetFullPath_LowercasesAndJoinsModuleProviderName()
    {
        var stream = new InputStream<TestConsumer>("Test", "Orders", StreamTypes.Events);

        stream.GetFullPath("MyModule").Should().Be("mymodule.p-test.orders");
    }

    private class TestProvider : InputStreamProvider
    {
        public override string Key => "test";

        public override IEnumerable<InputStream> GetInputStreams() => throw new NotImplementedException();

        public InputStream<TConsumer> DefineTestStream<TConsumer>(string name, StreamTypes sources, Action<InputStream>? configure = null)
            => DefineStream<TConsumer>(name, sources, configure);
    }

    private class TestConsumer
    {
    }
}
