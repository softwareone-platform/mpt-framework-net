using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class GenericCreatedEventTests
{
    [Fact]
    public void MakeMessage_StampsExpectedRoutingAndContent()
    {
        var entity = new TestEntity { Id = "acct-1" };

        var @event = new GenericCreatedEvent<TestEntity>("billing", entity, new PlatformEventPermissionsBuilder());
        var message = @event.MakeMessage();

        @event.EventKey.Should().Be(PlatformEventConstants.EVENT_CREATED);
        message.Routing.SourceModule.Should().Be("billing");
        message.Routing.Event.Should().Be(PlatformEventConstants.EVENT_CREATED);
        message.Routing.Entity.Should().Be(nameof(TestEntity));
        message.Info.Summary.Should().Be($"{nameof(TestEntity)} created");
        message.Info.Description.Should().Contain("was created by");
        message.Objects.Should().ContainSingle()
            .Which.Category.Should().Be(EventMessageObjectCategory.CurrentEntity);
        message.Objects[0].Id.Should().Be("acct-1");
    }
}
