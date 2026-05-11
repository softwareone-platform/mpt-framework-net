using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class EventMessageRoutingTests
{
    [Fact]
    public void ToPath_BuildsPlatformPathWithCamelCasedSegments()
    {
        var routing = new EventMessageRouting
        {
            Stream = StreamTypes.Events,
            SourceModule = "Billing",
            Entity = "Invoice",
            Event = "Created",
        };

        routing.ToPath().Should().Be("platform.billing.invoice.Created");
    }

    [Fact]
    public void ToPath_OnlyLowercasesTheFirstCharacterOfModuleAndEntity()
    {
        // The Event segment is intentionally NOT lowercased — the existing format keeps the
        // event name verbatim so subscribers can match on canonical casing.
        var routing = new EventMessageRouting
        {
            Stream = StreamTypes.Events,
            SourceModule = "PriceBook",
            Entity = "PriceListItem",
            Event = "PriceUpdated",
        };

        routing.ToPath().Should().Be("platform.priceBook.priceListItem.PriceUpdated");
    }

    [Fact]
    public void TargetModules_DefaultsToEmpty()
    {
        var routing = new EventMessageRouting();

        routing.TargetModules.Should().BeEmpty();
    }
}
