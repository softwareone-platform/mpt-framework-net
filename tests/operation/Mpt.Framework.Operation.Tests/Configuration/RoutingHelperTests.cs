using FluentAssertions;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Tests.Configuration;

public class RoutingHelperTests
{
    [Fact]
    public void BuildFilter_ProducesSqlRuleScopedToTheTargetName()
    {
        var descriptor = MakeDescriptor("invoices");

        var rule = RoutingHelper.BuildFilter(descriptor, MessageGroup.Main);

        rule.Name.Should().Be(RoutingHelper.FilterName);
        rule.Filter.ToString().Should().Contain($"{RoutingHelper.TargetHeaderName} = '{descriptor.GetTargetName(MessageGroup.Main)}'");
    }

    [Fact]
    public void BuildFilter_EmbedsTargetNameForEachMessageGroup()
    {
        var descriptor = MakeDescriptor("orders");

        foreach (var group in Enum.GetValues<MessageGroup>())
        {
            var rule = RoutingHelper.BuildFilter(descriptor, group);

            rule.Filter.ToString().Should().Contain(descriptor.GetTargetName(group));
        }
    }

    private static OperationDescriptor MakeDescriptor(string name) => new()
    {
        Name = name,
        ModuleCode = "test-module",
        GlobalPrefix = null,
        SagaType = typeof(object),
        ImplementationType = typeof(object),
        OperationType = typeof(object),
        TaskType = typeof(object),
    };
}
