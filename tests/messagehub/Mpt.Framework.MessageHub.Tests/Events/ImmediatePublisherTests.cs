using FluentAssertions;
using NSubstitute;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class ImmediatePublisherTests
{
    [Fact]
    public async Task PublishAsync_DelegatesToUnderlyingMessageHubPublisher()
    {
        var underlying = Substitute.For<IMessageHubPublisher>();
        var publisher = new ImmediatePlatformMessagePublisher(underlying);
        var message = MakeMessage();

        await publisher.PublishAsync(new TracedTransport<EventMessage>(message, null), CancellationToken.None);

        await underlying.Received(1).PublishAsync(message, Arg.Any<CancellationToken>());
    }

    private static EventMessage MakeMessage() => new()
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
