using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mpt.Framework.MessageHub.Tests.Events;

/// <summary>
/// End-to-end check: register a <see cref="GenericCreatedEvent{TEntity}"/> with the
/// scoped <see cref="IPlatformEventEmitter"/>, call <see cref="IPlatformEventEmitter.EmitAsync"/>,
/// and verify the corresponding <see cref="EventMessage"/> arrives at a MassTransit consumer
/// over the in-memory transport.
/// </summary>
public class InMemoryEventRoundTripTests
{
    private const string ProducerModule = "producer-module";

    [Fact]
    public async Task EmitAsync_GenericCreatedEvent_DeliversMessageToConsumer()
    {
        var sink = new EventSink();
        await using var host = await StartHostAsync(sink);

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var emitter = scope.ServiceProvider.GetRequiredService<IPlatformEventEmitter>();
            var entity = new TestEntity { Id = "acct-1" };
            emitter.Register(new GenericCreatedEvent<TestEntity>(
                ProducerModule, entity, new PlatformEventPermissionsBuilder()));
            await emitter.EmitAsync(CancellationToken.None);
        }

        await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(3));
        sink.Received.Should().ContainSingle()
            .Which.Routing.Event.Should().Be(PlatformEventConstants.EVENT_CREATED);
        sink.Received[0].Routing.Entity.Should().Be(nameof(TestEntity));
        sink.Received[0].Routing.SourceModule.Should().Be(ProducerModule);
    }

    private static async Task<TestHost> StartHostAsync(EventSink sink)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(sink);
        services.AddScoped<TestEventConsumer>();

        services.AddMessageHub(ProducerModule, hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
            hub.ConfigureInput<TestEventStreamProvider>();
        });

        var serviceProvider = services.BuildServiceProvider();
        var bus = (IBusControl)serviceProvider.GetRequiredService<IMessageHubBus>();
        await bus.StartAsync();
        return new TestHost(serviceProvider, bus);
    }

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

    public sealed class TestEventStreamProvider : InputStreamProvider
    {
        public override string Key => "main";

        public override IEnumerable<InputStream> GetInputStreams()
        {
            yield return DefineStream<TestEventConsumer>("events", StreamTypes.Events, input =>
            {
                input.Filter.AllowOwnEvents = true;
            });
        }
    }

    public class TestEventConsumer(EventSink sink) : IConsumer<EventMessage>
    {
        public Task Consume(ConsumeContext<EventMessage> context)
        {
            sink.Add(context.Message);
            return Task.CompletedTask;
        }
    }

    public class EventSink
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
                    return;
                await Task.Delay(25);
            }
            await Task.Delay(100);
        }
    }
}
