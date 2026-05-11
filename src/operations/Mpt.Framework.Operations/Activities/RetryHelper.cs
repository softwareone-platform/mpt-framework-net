using MassTransit;
using MassTransit.AzureServiceBusTransport;
using Mpt.Framework.Operations.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operations.Activities;

[ExcludeFromCodeCoverage(Justification = "Broker specific logic")]
internal static class RetryHelper<TOperation>
    where TOperation : IOperationContract
{
    public static bool ShouldRetry(
        IOperationProvider operationProvider,
        Func<OperationDescriptor, GroupProcessingOptions> configSelector,
        ConsumeContext consumeContext,
        Exception exception)
    {
        if (!operationProvider.TryGetDescriptor<TOperation>(out var descriptor))
            return false;

        var options = configSelector(descriptor!);
        if (options?.Retry is not { } retry)
            return false;

        // IMPORTANT:
        // Uses Azure Service Bus broker DeliveryCount.
        // This intentionally ignores MassTransit retry/redelivery counters.
        // We handle exception in usual way on very last attempt. Message sent to DLQ by MT, not by broker.
        if (GetBrokerDeliveryCount(consumeContext) >= options.MaxAttempts)
            return false;

        return retry.Filter(exception);
    }

    private static int GetBrokerDeliveryCount(ConsumeContext context)
    {
        if (context.ReceiveContext is ServiceBusReceiveContext sbContext)
            return sbContext.DeliveryCount;

        return 1; // defensive default
    }
}
