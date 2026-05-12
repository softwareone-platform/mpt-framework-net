using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class CustomEventTests
{
    [Fact]
    public void EventKey_DefaultsToUnconfiguredUntilCustomized()
    {
        var entity = new TestEntity { Id = "acct-1" };

        var @event = new CustomEvent<TestEntity>("billing", entity, null, new PlatformEventPermissionsBuilder());

        @event.EventKey.Should().Be("unconfigured_event");
    }

    [Fact]
    public void MakeMessage_AfterCustomize_UsesDescriptorEventKeySummaryAndDescription()
    {
        var entity = new TestEntity { Id = "acct-1" };
        var @event = new CustomEvent<TestEntity>("billing", entity, null, new PlatformEventPermissionsBuilder());

        @event.Customize(d =>
        {
            d.EventKey = "billing.reconciled";
            d.Summary = "Billing reconciled";
            d.Description = "The billing record was reconciled.";
        });

        var message = @event.MakeMessage();

        message.Routing.Event.Should().Be("billing.reconciled");
        message.Info.Summary.Should().Be("Billing reconciled");
        message.Info.Description.Should().Be("The billing record was reconciled.");
    }
}
