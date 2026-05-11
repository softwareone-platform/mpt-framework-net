using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mpt.Framework.MessageHub.Tests.Functionality;

/// <summary>
/// End-to-end tests using the in-memory transport. Each test stands up a fresh DI container,
/// registers a single <see cref="ConfigurableStreamProvider"/> with a per-test filter, publishes
/// one or more <see cref="EventMessage"/> instances, and asserts which ones the consumer received.
/// </summary>
public class InMemoryRoundTripTests
{
    private const string ConsumerModule = "consumer-module";
    private const string OtherModule = "other-module";

    [Fact]
    public async Task Publish_MatchingStream_ConsumerReceivesEventMessage()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink, new ConfigurableStreamProvider());

        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Account"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.Entity.Should().Be("Account");
    }

    [Fact]
    public async Task Publish_FromOwnModule_FilteredOut_WhenAllowOwnEventsIsFalse()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink, new ConfigurableStreamProvider());

        await PublishAsync(host, MakeEvent(sourceModule: ConsumerModule, entity: "Account"));
        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Account"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle("the own-module event should be filtered out")
            .Which.Routing.SourceModule.Should().Be(OtherModule);
    }

    [Fact]
    public async Task Publish_FromOwnModule_Delivered_WhenAllowOwnEventsIsTrue()
    {
        var sink = new EventSink();
        var provider = new ConfigurableStreamProvider(f => f.AllowOwnEvents = true);
        await using var host = await StartHostAsync(sink, provider);

        await PublishAsync(host, MakeEvent(sourceModule: ConsumerModule, entity: "Account"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.SourceModule.Should().Be(ConsumerModule);
    }

    [Fact]
    public async Task Publish_EntityNotInFilter_ConsumerSkipsIt()
    {
        var sink = new EventSink();
        var provider = new ConfigurableStreamProvider(f => f.Entities = ["Account"]);
        await using var host = await StartHostAsync(sink, provider);

        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Order"));
        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Account"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.Entity.Should().Be("Account");
    }

    [Fact]
    public async Task Publish_SourceModuleNotInFilterList_ConsumerSkipsIt()
    {
        var sink = new EventSink();
        var provider = new ConfigurableStreamProvider(f => f.Modules = ["allowed-module"]);
        await using var host = await StartHostAsync(sink, provider);

        await PublishAsync(host, MakeEvent(sourceModule: "blocked-module", entity: "Account"));
        await PublishAsync(host, MakeEvent(sourceModule: "allowed-module", entity: "Account"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.SourceModule.Should().Be("allowed-module");
    }

    [Fact]
    public async Task Publish_EventNotInFilterList_ConsumerSkipsIt()
    {
        var sink = new EventSink();
        var provider = new ConfigurableStreamProvider(f => f.Events = ["Created"]);
        await using var host = await StartHostAsync(sink, provider);

        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Account", eventName: "Deleted"));
        await PublishAsync(host, MakeEvent(sourceModule: OtherModule, entity: "Account", eventName: "Created"));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.Event.Should().Be("Created");
    }

    [Fact]
    public async Task Publish_StreamTypeNotInStreamSources_ConsumerSkipsIt()
    {
        var sink = new EventSink();
        // Stream only accepts Events; publishing a Sync event should be filtered out.
        var provider = new ConfigurableStreamProvider(sources: StreamTypes.Events);
        await using var host = await StartHostAsync(sink, provider);

        await PublishAsync(host, MakeEvent(OtherModule, "Account", stream: StreamTypes.Sync));
        await PublishAsync(host, MakeEvent(OtherModule, "Account", stream: StreamTypes.Events));

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.Stream.Should().Be(StreamTypes.Events);
    }

    [Fact]
    public async Task Publish_InvokesOnMessagePublishingHook()
    {
        var sink = new EventSink();
        var hookHits = new List<string>();

        await using var host = await StartHostAsync(
            sink,
            new ConfigurableStreamProvider(),
            customize: builder => builder.OnMessagePublishing = msg => hookHits.Add(msg.Routing.Entity));

        await PublishAsync(host, MakeEvent(OtherModule, "Account"));

        hookHits.Should().BeEquivalentTo(["Account"]);
    }

    private static async Task<TestHost> StartHostAsync(
        EventSink sink,
        ConfigurableStreamProvider provider,
        Action<MessageHubBuilder>? customize = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sink);
        services.AddScoped<EventConsumer>();

        services.AddMessageHub(ConsumerModule, hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
            hub.ConfigureInput(provider);
            customize?.Invoke(hub);
        });

        var serviceProvider = services.BuildServiceProvider();
        var bus = (IBusControl)serviceProvider.GetRequiredService<IMessageHubBus>();
        await bus.StartAsync();
        return new TestHost(serviceProvider, bus);
    }

    private static Task PublishAsync(TestHost host, EventMessage message)
        => host.Services.GetRequiredService<IMessageHubPublisher>().PublishAsync(message, CancellationToken.None);

    private static EventMessage MakeEvent(
        string sourceModule,
        string entity,
        string eventName = "Created",
        StreamTypes stream = StreamTypes.Events) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        Info = new EventMessageInfo { Summary = "test" },
        Objects =
        [
            new EventMessageObject
            {
                Id = "1",
                Key = "entity",
                Category = EventMessageObjectCategory.CurrentEntity,
                Data = new { name = "test" },
            }
        ],
        Routing = new EventMessageRouting
        {
            Stream = stream,
            SourceModule = sourceModule,
            Entity = entity,
            Event = eventName,
        }
    };

    private sealed class TestHost(ServiceProvider provider, IBusControl bus) : IAsyncDisposable
    {
        public IServiceProvider Services => provider;

        public async ValueTask DisposeAsync()
        {
            await bus.StopAsync();
            await provider.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// One-stream provider whose filter (and optionally accepted stream types) is set up by the
    /// test that constructs it — keeps each test focused on its scenario without a bespoke
    /// provider class per case.
    /// </summary>
    private sealed class ConfigurableStreamProvider(Action<InputStreamFilter>? configureFilter = null, StreamTypes sources = StreamTypes.Events)
        : InputStreamProvider
    {
        public override string Key => "main";

        public override IEnumerable<InputStream> GetInputStreams()
        {
            yield return DefineStream<EventConsumer>("test-stream", sources, input =>
            {
                configureFilter?.Invoke(input.Filter);
            });
        }
    }

    private class EventConsumer(EventSink sink) : IConsumer<EventMessage>
    {
        public Task Consume(ConsumeContext<EventMessage> context)
        {
            sink.Add(context.Message);
            return Task.CompletedTask;
        }
    }

    private class EventSink
    {
        private readonly List<EventMessage> _received = [];
        private readonly Lock _lock = new();

        public IReadOnlyList<EventMessage> Received
        {
            get { lock (_lock) return [.. _received]; }
        }

        public void Add(EventMessage message)
        {
            lock (_lock) _received.Add(message);
        }

        public async Task WaitForCountAsync(int expectedCount, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while (Received.Count < expectedCount)
            {
                if (DateTime.UtcNow - start > timeout)
                    return; // let the test assertion produce the failure message
                await Task.Delay(25);
            }
            // Give the bus a beat to deliver any unintended extras so the test can catch
            // over-delivery (e.g. a stream that should have filtered a message but didn't).
            await Task.Delay(100);
        }
    }
}
