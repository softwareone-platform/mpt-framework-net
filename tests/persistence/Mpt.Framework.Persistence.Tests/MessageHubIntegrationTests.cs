using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Tests.Fixtures;
using NSubstitute;
using System.Runtime.CompilerServices;

namespace Mpt.Framework.Persistence.Tests;

public class MessageHubIntegrationTests
{
    [Fact]
    public async Task AfterSaveChanges_publishes_every_produced_event_via_IMessageHubPublisher()
    {
        var publisher = Substitute.For<IMessageHubPublisher>();
        var producer = new EmittingProducer();

        using var services = PersistenceFixture.Build(configureServices: s =>
        {
            s.AddSingleton(publisher);
            s.AddSingleton<IEntityEventProducer<WidgetView>>(producer);
        });
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });
        await uow.SaveChangesAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<EventMessage>(m => m.Id == "evt-w1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AfterSaveChanges_without_IMessageHubPublisher_still_completes_cleanly()
    {
        var producer = new EmittingProducer();

        using var services = PersistenceFixture.Build(configureServices: s =>
            s.AddSingleton<IEntityEventProducer<WidgetView>>(producer));
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });

        // Should not throw even though no IMessageHubPublisher is registered.
        var act = async () => await uow.SaveChangesAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AfterSaveChanges_does_not_publish_when_ShouldProduceOn_is_false()
    {
        var publisher = Substitute.For<IMessageHubPublisher>();
        var producer = new EmittingProducer { ProduceCreates = false };

        using var services = PersistenceFixture.Build(configureServices: s =>
        {
            s.AddSingleton(publisher);
            s.AddSingleton<IEntityEventProducer<WidgetView>>(producer);
        });
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });
        await uow.SaveChangesAsync(CancellationToken.None);

        await publisher.DidNotReceive().PublishAsync(Arg.Any<EventMessage>(), Arg.Any<CancellationToken>());
    }

    private sealed class EmittingProducer : IEntityEventProducer<WidgetView>
    {
        public bool ProduceCreates { get; set; } = true;

        public bool ShouldProduceOn(EntityAction action) => action switch
        {
            EntityAction.Create => ProduceCreates,
            _ => false,
        };

        public async IAsyncEnumerable<EventMessage> ProduceAsync(EntityAction action, WidgetView current, WidgetView? original, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new EventMessage
            {
                Id = $"evt-{current.Id}",
                Timestamp = DateTimeOffset.UtcNow,
                Routing = new EventMessageRouting
                {
                    Stream = StreamTypes.Events,
                    SourceModule = "test-module",
                    Entity = "Widget",
                    Event = "created",
                },
                Objects =
                [
                    new EventMessageObject
                    {
                        Id = current.Id,
                        Key = current.Id,
                        Type = "Widget",
                        Category = EventMessageObjectCategory.CurrentEntity,
                        Data = current,
                    },
                ],
                Info = new EventMessageInfo { Summary = $"Widget {current.Id} created" },
            };
        }
    }
}
