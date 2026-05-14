using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class PlatformEventEmitterTests
{
    [Fact]
    public async Task EmitAsync_PublishesOneMessagePerRegisteredEvent()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-1"));
        emitter.Register(MakeEvent("acct-2"));

        await emitter.EmitAsync(CancellationToken.None);

        await publisher.Received(2).PublishAsync(Arg.Any<TracedTransport<EventMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitSingleAsync_BypassesQueue()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-queued"));
        await emitter.EmitSingleAsync(MakeEvent("acct-single"), CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TracedTransport<EventMessage>>(t => t.Message.Objects[0].Id == "acct-single"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reset_DiscardsQueuedEventsWithoutPublishing()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-1"));
        emitter.Reset();

        await emitter.EmitAsync(CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task EmitAsync_SkipsSuppressedEvents()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        var suppressed = MakeEvent("acct-1");
        suppressed.Customize(d => d.IsSuppressed = true);
        emitter.Register(suppressed);

        await emitter.EmitAsync(CancellationToken.None);

        await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task EmitAsync_StampsActorObject_WhenActorProducerReturnsNonNull()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var actorProducer = Substitute.For<IPlatformEventActorProducer>();
        actorProducer.GetActor(Arg.Any<CancellationToken>())
            .Returns(new EventMessageActor { Id = "user-1", Name = "Alice" });

        var sp = new ServiceCollection()
            .AddSingleton(actorProducer)
            .BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-1"));
        await emitter.EmitAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TracedTransport<EventMessage>>(t =>
                t.Message.Objects.Any(o => o.Category == EventMessageObjectCategory.ActorInfo && o.Id == "user-1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitAsync_SkipsActorStamping_WhenActorProducerReturnsNull()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var actorProducer = Substitute.For<IPlatformEventActorProducer>();
        actorProducer.GetActor(Arg.Any<CancellationToken>()).Returns((EventMessageActor?)null);

        var sp = new ServiceCollection()
            .AddSingleton(actorProducer)
            .BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-1"));
        await emitter.EmitAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TracedTransport<EventMessage>>(t =>
                t.Message.Objects.All(o => o.Category != EventMessageObjectCategory.ActorInfo)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitAsync_SkipsActorStamping_WhenNoActorProducerRegistered()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        emitter.Register(MakeEvent("acct-1"));
        await emitter.EmitAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TracedTransport<EventMessage>>(t =>
                t.Message.Objects.All(o => o.Category != EventMessageObjectCategory.ActorInfo)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WithEnumerable_EnqueuesEachEventForEmission()
    {
        var publisher = Substitute.For<IPlatformMessagePublisher>();
        var sp = new ServiceCollection().BuildServiceProvider();
        var emitter = new PlatformEventEmitter(publisher, sp);

        IEnumerable<IPlatformEvent> batch = [MakeEvent("acct-1"), MakeEvent("acct-2"), MakeEvent("acct-3")];
        emitter.Register(batch);

        await emitter.EmitAsync(CancellationToken.None);

        await publisher.Received(3).PublishAsync(Arg.Any<TracedTransport<EventMessage>>(), Arg.Any<CancellationToken>());
    }

    private static GenericCreatedEvent<TestEntity> MakeEvent(string id) =>
        new("billing", new TestEntity { Id = id }, new PlatformEventPermissionsBuilder());
}
