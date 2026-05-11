using FluentAssertions;
using MassTransit;
using Mpt.Framework.MessageHub.Internal;

namespace Mpt.Framework.MessageHub.Tests;

public class StreamRoutingHelperTests
{
    [Fact]
    public void GetOutputAttributes_NullRouting_Throws()
    {
        var message = new EventMessage { Routing = null! };

        var act = () => StreamRoutingHelper.GetOutputAttributes(message).ToList();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOutputAttributes_ReturnsHeadersFromRouting()
    {
        var message = new EventMessage
        {
            Routing = new EventMessageRouting
            {
                Stream = StreamTypes.Sync,
                SourceModule = "SourceModule",
                Entity = "Entity",
                Event = "Event",
                TargetModules = ["TargetModule"]
            }
        };

        var result = StreamRoutingHelper.GetOutputAttributes(message).ToList();

        result.Should().BeEquivalentTo(new List<(string, string)>
        {
            (MessageHubHeaders.StreamType, "Sync"),
            (MessageHubHeaders.SourceModule, "SourceModule"),
            (MessageHubHeaders.Entity, "Entity"),
            (MessageHubHeaders.Event, "Event"),
            (MessageHubHeaders.TargetModules, "|targetmodule|"),
        }, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void GetOutputAttributes_OmitsTargetModulesHeaderWhenEmpty()
    {
        var message = new EventMessage
        {
            Routing = new EventMessageRouting
            {
                Stream = StreamTypes.Sync,
                SourceModule = "SourceModule",
                Entity = "Entity",
                Event = "Event"
            }
        };

        var result = StreamRoutingHelper.GetOutputAttributes(message).ToList();

        result.Should().NotContain(r => r.key == MessageHubHeaders.TargetModules);
        result.Should().HaveCount(4);
    }

    [Fact]
    public void GetOutputAttributes_LowercasesAndPipeDelimitsMultipleTargetModules()
    {
        var message = new EventMessage
        {
            Routing = new EventMessageRouting
            {
                Stream = StreamTypes.Sync,
                SourceModule = "SourceModule",
                Entity = "Entity",
                Event = "Event",
                TargetModules = ["ModuleA", "ModuleB"]
            }
        };

        var result = StreamRoutingHelper.GetOutputAttributes(message).ToList();

        var targetHeader = result.Single(r => r.key == MessageHubHeaders.TargetModules);
        targetHeader.value.Should().Be("|modulea|moduleb|");
    }

    [Fact]
    public void EncodeTargetModules_LowercasesAndPipeDelimits()
    {
        var result = StreamRoutingHelper.EncodeTargetModules(["Exchange", "Catalog", "CRM"]);

        result.Should().Be("|exchange|catalog|crm|");
    }

    [Fact]
    public void BuildInputFilter_BuildsSqlFilterFromAllFilterFields()
    {
        var stream = new InputStream<FakeConsumer>("test", "test", StreamTypes.Sync)
        {
            Filter = new InputStreamFilter
            {
                Modules = ["Module1"],
                Entities = ["Entity1"],
                Events = ["Event1"]
            }
        };

        var result = StreamRoutingHelper.BuildInputFilter("TestModule", stream);

        var th = MessageHubHeaders.TargetModules;
        var expected = $"(({th} IS NULL) " +
            $"OR ({th} LIKE '%|testmodule|%')" +
            $") AND {MessageHubHeaders.StreamType} IN ('Sync')" +
            $" AND {MessageHubHeaders.SourceModule} IN ('Module1')" +
            $" AND {MessageHubHeaders.Entity} IN ('Entity1')" +
            $" AND {MessageHubHeaders.Event} IN ('Event1')";

        result.Should().Be(expected);
    }

    [Fact]
    public void BuildInputFilter_DisablingStream_ReturnsAlwaysFalse()
    {
        var stream = new InputStream<FakeConsumer>("test", "test", StreamTypes.Sync)
        {
            State = InputStreamState.Disabling
        };

        var result = StreamRoutingHelper.BuildInputFilter("TestModule", stream);

        result.Should().Be("1=0");
    }

    [Fact]
    public void BuildInputFilter_NoModuleFilter_AddsOwnModuleExclusion()
    {
        var stream = new InputStream<FakeConsumer>("test", "test", StreamTypes.Sync);

        var result = StreamRoutingHelper.BuildInputFilter("TestModule", stream);

        result.Should().Contain($"{MessageHubHeaders.SourceModule} != 'TestModule'");
    }

    [Fact]
    public void BuildInputFilter_NoModuleFilterButAllowOwnEvents_OmitsOwnModuleExclusion()
    {
        var stream = new InputStream<FakeConsumer>("test", "test", StreamTypes.Sync)
        {
            Filter = new InputStreamFilter { AllowOwnEvents = true }
        };

        var result = StreamRoutingHelper.BuildInputFilter("TestModule", stream);

        result.Should().NotContain($"{MessageHubHeaders.SourceModule} != ");
    }

    private class FakeConsumer : IConsumer<object>
    {
        public Task Consume(ConsumeContext<object> context) => Task.CompletedTask;
    }
}
