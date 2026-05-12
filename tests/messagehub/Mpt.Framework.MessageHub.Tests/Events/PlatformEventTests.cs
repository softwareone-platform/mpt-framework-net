using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class PlatformEventTests
{
    [Fact]
    public void MakeMessage_ProducesRoutingAndInfoFromOverrides()
    {
        var @event = new MinimalPlatformEvent();

        var message = @event.MakeMessage();

        message.Routing.SourceModule.Should().Be("test_module");
        message.Routing.Event.Should().Be("test_key");
        message.Info.Summary.Should().Be("object");
        message.Info.Description.Should().Be("object object-key");
        message.Objects.Should().HaveCount(1);
        message.Objects[0].Category.Should().Be(EventMessageObjectCategory.CurrentEntity);
    }

    [Fact]
    public void MakeMessage_WhenCustomizedWithAdditionalObjects_IncludesThem()
    {
        var @event = new MinimalPlatformEvent();
        var additional = new EventMessageObject
        {
            Id = "additional-id",
            Key = "additional-key",
            Category = EventMessageObjectCategory.AdditionalEntity,
            Data = new object()
        };

        @event.Customize(d => d.AdditionalObjects = [additional]);

        var message = @event.MakeMessage();

        message.Objects.Should().HaveCount(2);
        message.Objects.Should().Contain(o => o.Key == "additional-key" && o.Category == EventMessageObjectCategory.AdditionalEntity);
    }

    [Fact]
    public void MakeMessage_WhenDuplicateKey_Throws()
    {
        var @event = new MinimalPlatformEvent();
        var duplicate = new EventMessageObject
        {
            Id = "duplicate-id",
            Key = "object-key",
            Category = EventMessageObjectCategory.AdditionalEntity,
            Data = new object()
        };

        @event.Customize(d => d.AdditionalObjects = [duplicate]);

        var act = () => @event.MakeMessage();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*object-key*");
    }

    [Fact]
    public void MakeMessage_WhenCustomizedWithSummaryAndDescription_OverridesDefaults()
    {
        var @event = new MinimalPlatformEvent();

        @event.Customize(d =>
        {
            d.Summary = "Custom summary";
            d.Description = "Custom description";
        });

        var message = @event.MakeMessage();

        message.Info.Summary.Should().Be("Custom summary");
        message.Info.Description.Should().Be("Custom description");
    }

    [Fact]
    public void MakeMessage_WhenCustomizedWithEntityNameAndKey_UsesThemForSummaryAndDescription()
    {
        var @event = new MinimalPlatformEvent();

        @event.Customize(d =>
        {
            d.EntityName = "Custom Entity";
            d.EntityKey = "custom_key";
        });

        var message = @event.MakeMessage();

        message.Info.Summary.Should().Be("Custom Entity");
        message.Info.Description.Should().Be("Custom Entity custom_key");
    }

    [Fact]
    public void MakeMessage_WhenCustomEventKey_OverridesRoutingEvent()
    {
        var @event = new MinimalPlatformEvent();

        @event.Customize(d => d.EventKey = "alternative_key");

        var message = @event.MakeMessage();

        message.Routing.Event.Should().Be("alternative_key");
    }

    [Fact]
    public void TargetStream_DefaultsToEventsAndCanBeOverridden()
    {
        new MinimalPlatformEvent().TargetStreamProxy.Should().Be(StreamTypes.Events);
        new SyncStreamPlatformEvent().TargetStreamProxy.Should().Be(StreamTypes.Sync);
    }

    [Fact]
    public void SessionId_OverrideTakesPrecedenceOverMainObjectId()
    {
        var @event = new PlatformEventWithSessionAndPartition("explicit-session", "explicit-partition");

        var message = @event.MakeMessage();

        message.SessionId.Should().Be("explicit-session");
        message.PartitionKey.Should().Be("explicit-partition");
    }

    [Fact]
    public void IsSuppressed_ReportsCustomizationSuppressionFlag()
    {
        var @event = new MinimalPlatformEvent();
        @event.IsSuppressed.Should().BeFalse();

        @event.Customize(d => d.IsSuppressed = true);
        @event.IsSuppressed.Should().BeTrue();
    }

    public class MinimalPlatformEvent : PlatformEvent
    {
        public override string EventKey => "test_key";
        public override string ModuleName => "test_module";

        protected override EventMessageObject GetMainObject() => new()
        {
            Id = "object-id",
            Key = "object-key",
            Category = EventMessageObjectCategory.CurrentEntity,
            Data = new object()
        };

        protected override string GetEntityName() => "object";
        protected override string GetSummary(string entityName) => entityName;
        protected override string GetDescription(string entityName, string entityKey) => $"{entityName} {entityKey}";

        public StreamTypes TargetStreamProxy => TargetStream;
    }

    public class SyncStreamPlatformEvent : MinimalPlatformEvent
    {
        protected override StreamTypes TargetStream => StreamTypes.Sync;
    }

    public class PlatformEventWithSessionAndPartition(string sessionId, string partitionKey) : MinimalPlatformEvent
    {
        protected override string? GetSessionId() => sessionId;
        protected override string? GetPartitionKey() => partitionKey;
    }
}
