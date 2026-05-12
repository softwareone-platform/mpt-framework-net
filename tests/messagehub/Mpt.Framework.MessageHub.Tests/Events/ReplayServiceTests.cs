using FluentAssertions;
using NSubstitute;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class ReplayServiceTests
{
    [Fact]
    public async Task ReplayAsync_FirstAttempt_IncrementsReplaysAndRoutesToTargetModule()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        IPlatformMessageReplayService service = new PlatformMessageReplayService(publisher);
        var message = MakeMessage();

        var result = await service.ReplayAsync(message, module: "billing", CancellationToken.None);

        result.Should().BeTrue();
        message.Replays.Should().Be(1);
        message.Routing.Delay.Should().Be(TimeSpan.FromSeconds(3)); // Linear, attempt 1, InitialDelay 3s
        message.Routing.TargetModules.Should().BeEquivalentTo(["billing"]);
        await publisher.Received(1).PublishAsync(
            Arg.Is<TracedTransport<EventMessage>>(t => t.Message == message),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayAsync_LinearGrowth_DoublesByAttemptTwo()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        IPlatformMessageReplayService service = new PlatformMessageReplayService(publisher);
        var message = MakeMessage();

        await service.ReplayAsync(message, "billing", CancellationToken.None);
        await service.ReplayAsync(message, "billing", CancellationToken.None);

        message.Replays.Should().Be(2);
        message.Routing.Delay.Should().Be(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task ReplayAsync_OnceMaxAttemptsReached_ReturnsFalseWithoutPublishing()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        IPlatformMessageReplayService service = new PlatformMessageReplayService(publisher);
        var message = MakeMessage();
        message.Replays = 3; // matches default MaxAttempts

        var result = await service.ReplayAsync(message, "billing", CancellationToken.None);

        result.Should().BeFalse();
        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task ReplayAsync_OverridesStreamTypeWhenSpecified()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        IPlatformMessageReplayService service = new PlatformMessageReplayService(publisher);
        var message = MakeMessage(); // Routing.Stream = Events

        await service.ReplayAsync(message, "billing", StreamTypes.Sync, CancellationToken.None);

        message.Routing.Stream.Should().Be(StreamTypes.Sync);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ReplayAsync_RejectsBlankModule(string? module)
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        IPlatformMessageReplayService service = new PlatformMessageReplayService(publisher);
        var message = MakeMessage();

        var act = async () => await service.ReplayAsync(message, module!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
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
