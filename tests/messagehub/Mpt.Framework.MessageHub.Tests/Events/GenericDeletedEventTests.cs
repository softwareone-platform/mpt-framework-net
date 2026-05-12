using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class GenericDeletedEventTests
{
    [Fact]
    public void MakeMessage_AutomaticallyMarksIncomplete()
    {
        var entity = new TestEntity { Id = "acct-1" };

        var @event = new GenericDeletedEvent<TestEntity>("billing", entity, new PlatformEventPermissionsBuilder());
        var message = @event.MakeMessage();

        message.Routing.Event.Should().Be(PlatformEventConstants.EVENT_DELETED);
        message.Info.Summary.Should().Be($"{nameof(TestEntity)} deleted");
        message.Hints.HasFlag(EventHints.Incomplete).Should().BeTrue();
    }
}
