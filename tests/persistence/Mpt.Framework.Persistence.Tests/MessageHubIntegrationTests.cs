using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Tests.Fixtures;
using NSubstitute;

namespace Mpt.Framework.Persistence.Tests;

public class MessageHubIntegrationTests
{
    [Fact]
    public async Task AfterSaveChanges_registers_GenericCreatedEvent_with_emitter()
    {
        var emitter = Substitute.For<IPlatformEventEmitter>();

        using var services = PersistenceFixture.Build(configureServices: s =>
        {
            s.AddSingleton(emitter);
            s.AddScoped<IEntityEventProducer<WidgetView>, CreatedOnlyProducer>();
        });
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });
        await uow.SaveChangesAsync(CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericCreatedEvent<WidgetView>)
            && ((GenericCreatedEvent<WidgetView>)e).Entity.Id == "w1"));

        await emitter.Received(1).EmitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AfterSaveChanges_without_emitter_still_completes_cleanly()
    {
        using var services = PersistenceFixture.Build(configureServices: s =>
            s.AddScoped<IEntityEventProducer<WidgetView>, CreatedOnlyProducer>());
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });

        var act = async () => await uow.SaveChangesAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AfterSaveChanges_skips_emitter_when_ShouldProduceOn_returns_false()
    {
        var emitter = Substitute.For<IPlatformEventEmitter>();

        using var services = PersistenceFixture.Build(configureServices: s =>
        {
            s.AddSingleton(emitter);
            s.AddScoped<IEntityEventProducer<WidgetView>, OptOutProducer>();
        });
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });
        await uow.SaveChangesAsync(CancellationToken.None);

        emitter.DidNotReceiveWithAnyArgs().Register((IPlatformEvent)default!);
        await emitter.Received(1).EmitAsync(Arg.Any<CancellationToken>());
    }

    private sealed class CreatedOnlyProducer(IServiceProvider sp) : EntityEventProducer<WidgetView>(sp)
    {
        protected override void ConfigureEvents(IEventPolicy<WidgetView> policy)
            => policy.Define(EntityAction.Create);
    }

    private sealed class OptOutProducer(IServiceProvider sp) : EntityEventProducer<WidgetView>(sp);
}
