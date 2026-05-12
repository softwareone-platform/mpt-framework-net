using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class GenericUpdatedEventTests
{
    [Fact]
    public void MakeMessage_WithoutOriginal_HasNoOriginalEntityObject()
    {
        var entity = new TestEntity { Id = "acct-1" };

        var @event = new GenericUpdatedEvent<TestEntity>("billing", entity, new PlatformEventPermissionsBuilder());
        var message = @event.MakeMessage();

        message.Routing.Event.Should().Be(PlatformEventConstants.EVENT_UPDATED);
        message.Info.Summary.Should().Be($"{nameof(TestEntity)} updated");
        message.Objects.Should().ContainSingle()
            .Which.Category.Should().Be(EventMessageObjectCategory.CurrentEntity);
    }

    [Fact]
    public void MakeMessage_WithOriginal_AppendsOriginalEntityObject()
    {
        var entity = new TestEntity { Id = "acct-1", Status = "new" };
        var original = new TestEntity { Id = "acct-1", Status = "old" };

        var @event = new GenericUpdatedEvent<TestEntity>("billing", entity, original, new PlatformEventPermissionsBuilder());
        var message = @event.MakeMessage();

        message.Objects.Should().HaveCount(2);
        message.Objects.Should().Contain(o => o.Category == EventMessageObjectCategory.CurrentEntity);
        message.Objects.Should().Contain(o => o.Category == EventMessageObjectCategory.OriginalEntity);
        message.Objects.First(o => o.Category == EventMessageObjectCategory.OriginalEntity)
            .Key.Should().Be($"original{nameof(TestEntity)}");
    }
}
