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
    public void DefineStream_WithoutConfigureCallback_LeavesDefaults()
    {
        var provider = new TestProvider();

        var stream = provider.DefineTestStream<TestConsumer>("orders", StreamTypes.Events);

        stream.Filter.Modules.Should().BeNull();
        stream.Filter.Entities.Should().BeNull();
        stream.Filter.Events.Should().BeNull();
        stream.Filter.AllowOwnEvents.Should().BeFalse();
        stream.State.Should().Be(InputStreamState.Active);
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
