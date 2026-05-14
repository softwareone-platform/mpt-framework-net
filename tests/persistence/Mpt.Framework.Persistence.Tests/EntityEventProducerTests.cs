using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub;
using NSubstitute;

namespace Mpt.Framework.Persistence.Tests;

public class EntityEventProducerTests
{
    [Fact]
    public void ShouldProduceOn_ReturnsTrueOnlyForActionsConfiguredByConfigureEvents()
    {
        var (producer, _) = BuildProducer();

        producer.ShouldProduceOn(EntityAction.Create).Should().BeTrue();
        producer.ShouldProduceOn(EntityAction.Update).Should().BeTrue();
        producer.ShouldProduceOn(EntityAction.Delete).Should().BeTrue();
    }

    [Fact]
    public async Task ProduceCreatedEvents_RegistersGenericCreatedEventWithEmitter()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1" };

        await producer.ProduceCreatedEvents(entity, CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericCreatedEvent<TestEntity>)));
    }

    [Fact]
    public async Task ProduceUpdatedEvents_RegistersGenericUpdatedEventWithOriginal()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1", Status = "new" };
        var original = new TestEntity { Id = "acct-1", Status = "old" };

        await producer.ProduceUpdatedEvents(entity, original, CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericUpdatedEvent<TestEntity>)
            && ((GenericUpdatedEvent<TestEntity>)e).Original == original));
    }

    [Fact]
    public async Task ProduceStatusChangedEvents_RegistersEventAndSuppressesSubsequentUpdate()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1", Status = "Active" };

        await producer.ProduceStatusChangedEvents(entity, null, e => e.Status ?? "(none)", CancellationToken.None);
        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericStatusChangedEvent<TestEntity>)));

        await producer.ProduceUpdatedEvents(entity, null, CancellationToken.None);
        emitter.DidNotReceive().Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericUpdatedEvent<TestEntity>)));
    }

    [Fact]
    public async Task ProduceDeletedEvents_RegistersGenericDeletedWithMinimalPayload()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1", Status = "Active" };

        await producer.ProduceDeletedEvents(entity, CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericDeletedEvent<TestEntity>)
            && ((GenericDeletedEvent<TestEntity>)e).Entity.Id == "acct-1"
            && ((GenericDeletedEvent<TestEntity>)e).Entity.Status == null));
    }

    [Fact]
    public async Task RegisterCustomEvent_ThenProduceCustomEvents_RegistersCustomEvent()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1" };

        producer.RegisterCustomEvent(entity, d =>
        {
            d.EventKey = "billing.reconciled";
            d.Summary = "Billing reconciled";
            d.Description = "The billing record was reconciled.";
        });

        await producer.ProduceCustomEvents(entity, null, CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(CustomEvent<TestEntity>)));
    }

    [Theory]
    [InlineData("", "Summary", "Description", "EventKey is required")]
    [InlineData("k", null, "Description", "Summary is required")]
    [InlineData("k", "Summary", null, "Description is required")]
    public async Task ProduceCustomEvents_ThrowsWhenDescriptorMissingMandatoryFields(string eventKey, string? summary, string? description, string expectedMessage)
    {
        var (producer, _) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1" };

        producer.RegisterCustomEvent(entity, d =>
        {
            d.EventKey = eventKey;
            d.Summary = summary;
            d.Description = description;
        });

        var act = async () => await producer.ProduceCustomEvents(entity, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(expectedMessage);
    }

    [Fact]
    public async Task CustomizeEvents_WithIsSuppressed_CausesProduceToSkipEmitterRegistration()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1" };

        producer.CustomizeEvents(entity, EntityEventTypes.Created, d => d.IsSuppressed = true);
        await producer.ProduceCreatedEvents(entity, CancellationToken.None);

        emitter.DidNotReceiveWithAnyArgs().Register((IPlatformEvent)default!);
    }

    [Fact]
    public async Task Reset_ClearsCustomizationsAndRegisteredCustomEvents()
    {
        var (producer, emitter) = BuildProducer();
        var entity = new TestEntity { Id = "acct-1" };

        producer.CustomizeEvents(entity, EntityEventTypes.Created, d => d.IsSuppressed = true);
        producer.RegisterCustomEvent(entity, d =>
        {
            d.EventKey = "k";
            d.Summary = "s";
            d.Description = "d";
        });

        producer.Reset();

        await producer.ProduceCreatedEvents(entity, CancellationToken.None);
        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.GetType() == typeof(GenericCreatedEvent<TestEntity>)));

        emitter.ClearReceivedCalls();
        await producer.ProduceCustomEvents(entity, null, CancellationToken.None);
        emitter.DidNotReceiveWithAnyArgs().Register((IPlatformEvent)default!);
    }

    [Fact]
    public async Task ProducedEventsCarrySourceModuleFromPersistenceBuilder()
    {
        var (producer, emitter) = BuildProducer(moduleCode: "billing");
        var entity = new TestEntity { Id = "acct-1" };

        await producer.ProduceCreatedEvents(entity, CancellationToken.None);

        emitter.Received(1).Register(Arg.Is<IPlatformEvent>(e =>
            e.MakeMessage().Routing.SourceModule == "billing"));
    }

    [Fact]
    public async Task ProducerIsNoOp_WhenIPlatformEventEmitterIsNotRegistered()
    {
        var services = new ServiceCollection();
        var pBuilder = new PersistenceBuilder(services, "billing");
        services.AddSingleton(pBuilder);
        var sp = services.BuildServiceProvider();
        IEntityEventProducer<TestEntity> producer = new TestProducer(sp);

        var entity = new TestEntity { Id = "acct-1" };

        var act = async () =>
        {
            await producer.ProduceCreatedEvents(entity, CancellationToken.None);
            await producer.ProduceUpdatedEvents(entity, null, CancellationToken.None);
            await producer.ProduceDeletedEvents(entity, CancellationToken.None);
            await producer.ProduceCustomEvents(entity, null, CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    private static (IEntityEventProducer<TestEntity> Producer, IPlatformEventEmitter Emitter) BuildProducer(string moduleCode = "billing")
    {
        var services = new ServiceCollection();
        var emitter = Substitute.For<IPlatformEventEmitter>();
        services.AddSingleton(emitter);
        services.AddSingleton(new PersistenceBuilder(services, moduleCode));
        var sp = services.BuildServiceProvider();
        return (new TestProducer(sp), emitter);
    }

    public class TestEntity : IPlatformEntity
    {
        public string Id { get; set; } = "test-id";
        public int Revision { get; set; }
        public string? Status { get; set; }
    }

    private sealed class TestProducer(IServiceProvider sp) : EntityEventProducer<TestEntity>(sp)
    {
        protected override void ConfigureEvents(IEventPolicy<TestEntity> context)
        {
            context.Define(EntityAction.Create);
            context.Define(EntityAction.Update);
            context.Define(EntityAction.Delete);
        }
    }
}
