using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class BackgroundPublisherTests
{
    [Fact]
    public async Task BackgroundPlatformMessagePublisher_EnqueuesToChannelService()
    {
        var channelService = Substitute.For<IPlatformEventChannelService>();
        var publisher = new BackgroundPlatformMessagePublisher(channelService);
        var transport = new TracedTransport<EventMessage>(MakeMessage(), null);

        await publisher.PublishAsync(transport, CancellationToken.None);

        await channelService.Received(1).AddMessage(transport, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlatformEventChannelService_WritesToTheChannel()
    {
        var channel = Channel.CreateUnbounded<TracedTransport<EventMessage>>();
        var service = new PlatformEventChannelService(channel, NullLogger<PlatformEventChannelService>.Instance);
        var transport = new TracedTransport<EventMessage>(MakeMessage(), null);

        await service.AddMessage(transport, CancellationToken.None);
        channel.Writer.Complete();

        var queued = await channel.Reader.ReadAllAsync().ToListAsync();
        queued.Should().ContainSingle().Which.Should().Be(transport);
    }

    [Fact]
    public async Task PlatformEventBackgroundService_DrainsChannelAndPublishesEachMessage()
    {
        var channel = Channel.CreateUnbounded<TracedTransport<EventMessage>>();
        var publisher = Substitute.For<IMessageHubPublisher>();
        var service = new PlatformEventBackgroundService(channel, publisher, NullLogger<PlatformEventBackgroundService>.Instance);

        await channel.Writer.WriteAsync(new TracedTransport<EventMessage>(MakeMessage("acct-1"), null));
        await channel.Writer.WriteAsync(new TracedTransport<EventMessage>(MakeMessage("acct-2"), null));

        await service.StartAsync(CancellationToken.None);
        await WaitUntilPublishedAsync(publisher, expected: 2, TimeSpan.FromSeconds(3));
        channel.Writer.Complete();
        await service.StopAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(Arg.Is<EventMessage>(m => m.Objects[0].Id == "acct-1"), Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(Arg.Is<EventMessage>(m => m.Objects[0].Id == "acct-2"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlatformEventBackgroundService_KeepsDrainingAfterPublisherThrows()
    {
        var channel = Channel.CreateUnbounded<TracedTransport<EventMessage>>();
        var publisher = Substitute.For<IMessageHubPublisher>();
        publisher.PublishAsync(Arg.Is<EventMessage>(m => m.Objects[0].Id == "boom"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var service = new PlatformEventBackgroundService(channel, publisher, NullLogger<PlatformEventBackgroundService>.Instance);

        await channel.Writer.WriteAsync(new TracedTransport<EventMessage>(MakeMessage("boom"), null));
        await channel.Writer.WriteAsync(new TracedTransport<EventMessage>(MakeMessage("ok"), null));

        await service.StartAsync(CancellationToken.None);
        await WaitUntilPublishedAsync(publisher, expected: 2, TimeSpan.FromSeconds(3));
        channel.Writer.Complete();
        await service.StopAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(Arg.Is<EventMessage>(m => m.Objects[0].Id == "ok"), Arg.Any<CancellationToken>());
    }

    private static async Task WaitUntilPublishedAsync(IMessageHubPublisher publisher, int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (publisher.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IMessageHubPublisher.PublishAsync)) >= expected)
                return;
            await Task.Delay(25);
        }
    }

    private static EventMessage MakeMessage(string id = "1") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        Info = new EventMessageInfo { Summary = "test" },
        Objects =
        [
            new EventMessageObject
            {
                Id = id,
                Key = "entity",
                Category = EventMessageObjectCategory.CurrentEntity,
                Data = new { },
            }
        ],
        Routing = new EventMessageRouting
        {
            Stream = StreamTypes.Events,
            SourceModule = "billing",
            Entity = "Account",
            Event = "created",
        }
    };
}
