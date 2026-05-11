using Azure.Messaging.ServiceBus.Administration;

namespace Mpt.Framework.Operation.Configuration;

internal static class RoutingHelper
{
    public const string FilterName = "target-filter";
    public const string TargetHeaderName = "Target";

    public static CreateRuleOptions BuildFilter(OperationDescriptor config, MessageGroup group)
    {
        return new CreateRuleOptions
        {
            Name = FilterName,
            Filter = new SqlRuleFilter($"{TargetHeaderName} = '{config.GetTargetName(group)}'")
        };
    }
}
