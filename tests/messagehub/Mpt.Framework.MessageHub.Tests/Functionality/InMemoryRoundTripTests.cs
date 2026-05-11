using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mpt.Framework.MessageHub.Tests.Functionality;

/// <summary>
/// End-to-end tests using the in-memory transport: a publisher emits an EventMessage and
/// a registered input stream's consumer should receive it (or be filtered out).
/// </summary>
public class InMemoryRoundTripTests
{
    [Fact]
    public async Task Publish_MatchingStream_ConsumerReceivesEventMessage()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink);

        var publisher = host.Services.GetRequiredService<IMessageHubPublisher>();
        await publisher.PublishAsync(MakeEvent(sourceModule: "other-module", entity: "Account"), CancellationToken.None);

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));

        sink.Received.Should().HaveCount(1);
        sink.Received[0].Routing.Entity.Should().Be("Account");
    }

    [Fact]
    public async Task Publish_FromOwnModule_FilteredOut_WhenAllowOwnEventsIsFalse()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink);

        var publisher = host.Services.GetRequiredService<IMessageHubPublisher>();
        // The default filter excludes the consumer's own module.
        await publisher.PublishAsync(MakeEvent(sourceModule: "consumer-module", entity: "Account"), CancellationToken.None);
        await publisher.PublishAsync(MakeEvent(sourceModule: "other-module", entity: "Account"), CancellationToken.None);

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));

        sink.Received.Should().HaveCount(1, "the own-module event should be filtered out");
        sink.Received[0].Routing.SourceModule.Should().Be("other-module");
    }

    [Fact]
    public async Task Publish_EntityNotInFilter_ConsumerSkipsIt()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink);

        var publisher = host.Services.GetRequiredService<IMessageHubPublisher>();
        await publisher.PublishAsync(MakeEvent(sourceModule: "other-module", entity: "Order"), CancellationToken.None);
        await publisher.PublishAsync(MakeEvent(sourceModule: "other-module", entity: "Account"), CancellationToken.None);

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));

        sink.Received.Should().HaveCount(1);
        sink.Received[0].Routing.Entity.Should().Be("Account");
    }

    [Fact]
    public async Task Publish_InvokesOnMessagePublishingHook()
    {
        var sink = new EventSink();
        var hookHits = new List<string>();

        await using var host = await StartHostAsync(sink, builder =>
        {
            builder.OnMessagePublishing = msg => hookHits.Add(msg.Routing.Entity);
        });

        var publisher = host.Services.GetRequiredService<IMessageHubPublisher>();
        await publisher.PublishAsync(MakeEvent("other-module", "Account"), CancellationToken.None);

        hookHits.Should().BeEquivalentTo(["Account"]);
    }

    private static async Task<TestHost> StartHostAsync(EventSink sink, Action<MessageHubBuilder>? customize = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sink);
        services.AddScoped<AccountConsumer>();

        services.AddMessageHub("consumer-module", hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
            hub.ConfigureInput<AccountStreamProvider>();
            customize?.Invoke(hub);
        });

        var provider = services.BuildServiceProvider();
        var bus = (IBusControl)provider.GetRequiredService<IMessageHubBus>();
        await bus.StartAsync();
        return new TestHost(provider, bus);
    }

    private static EventMessage MakeEvent(string sourceModule, string entity) => new()
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
            Stream = StreamTypes.Events,
            SourceModule = sourceModule,
            Entity = entity,
            Event = "Created",
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

    private class AccountStreamProvider : InputStreamProvider
    {
        public override string Key => "main";

        public override IEnumerable<InputStream> GetInputStreams()
        {
            yield return DefineStream<AccountConsumer>("accounts", StreamTypes.Events, input =>
            {
                input.Filter.Entities = ["Account"];
            });
        }
    }

    private class AccountConsumer(EventSink sink) : IConsumer<EventMessage>
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
        private readonly object _lock = new();

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
            // Give a beat for any unwanted extras (so we can detect over-delivery).
            await Task.Delay(100);
        }
    }
}
