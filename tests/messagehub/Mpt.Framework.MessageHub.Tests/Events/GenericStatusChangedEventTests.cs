using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class GenericStatusChangedEventTests
{
    [Fact]
    public void MakeMessage_EmbedsResolvedStatusInSummaryAndDescription()
    {
        var entity = new TestEntity { Id = "acct-1", Status = "Active" };

        var @event = new GenericStatusChangedEvent<TestEntity>(
            "billing", entity, new PlatformEventPermissionsBuilder(), e => e.Status ?? "(none)");

        @event.EventKey.Should().Be(PlatformEventConstants.EVENT_STATUS_CHANGED);

        var message = @event.MakeMessage();

        message.Info.Summary.Should().Be($"{nameof(TestEntity)} status changed to Active");
        message.Info.Description.Should().Contain("status was changed to Active");
    }

    [Fact]
    public void MakeMessage_WithOriginal_AppendsOriginalEntityObject()
    {
        var entity = new TestEntity { Id = "acct-1", Status = "Active" };
        var original = new TestEntity { Id = "acct-1", Status = "Pending" };

        var @event = new GenericStatusChangedEvent<TestEntity>(
            "billing", entity, original, new PlatformEventPermissionsBuilder(), e => e.Status ?? "(none)");
        var message = @event.MakeMessage();

        message.Objects.Should().Contain(o => o.Category == EventMessageObjectCategory.OriginalEntity);
    }
}
