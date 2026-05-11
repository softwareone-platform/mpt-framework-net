using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using Mpt.Framework.Operation.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Maintenance;

[ExcludeFromCodeCoverage]
internal class OperationCleanupService(OperationSettings configuration, IOperationProvider operationProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldRunCleanup())
            return;

        var adminClient = new ServiceBusAdministrationClient(configuration.ConnectionString!);

        var groupedConfigs = operationProvider.GetDescriptors().GroupBy(g => g.TopicName);

        foreach (var group in groupedConfigs)
        {
            var descriptors = group.ToList();
            await CleanupSubscriptionsForTopicAsync(adminClient, group.Key, descriptors, cancellationToken);
        }
    }

    private async Task CleanupSubscriptionsForTopicAsync(
        ServiceBusAdministrationClient adminClient,
        string topicName,
        List<OperationDescriptor> descriptors,
        CancellationToken cancellationToken)
    {
        await foreach (var subscription in adminClient.GetSubscriptionsAsync(topicName, cancellationToken: cancellationToken))
        {
            if (IsKnownSubscription(subscription.SubscriptionName, descriptors))
                continue;

            if (await ShouldDeleteSubscriptionAsync(adminClient, subscription, cancellationToken))
            {
                await adminClient.DeleteSubscriptionAsync(subscription.TopicName, subscription.SubscriptionName, cancellationToken);
            }
        }
    }

    private bool ShouldRunCleanup()
    {
        return configuration.Cleanup != OperationCleanupMode.None
            && configuration.ConnectionString != null
            && configuration.Transport == OperationTransport.ServiceBus;
    }

    private static bool IsKnownSubscription(string subscriptionName, List<OperationDescriptor> descriptors)
    {
        return descriptors.Exists(t => subscriptionName.StartsWith(t.Name));
    }

    private async Task<bool> ShouldDeleteSubscriptionAsync(
        ServiceBusAdministrationClient adminClient,
        SubscriptionProperties subscription,
        CancellationToken cancellationToken)
    {
        return configuration.Cleanup switch
        {
            OperationCleanupMode.DeleteAnyUnknown => true,
            OperationCleanupMode.DeleteEmptyUnknown => await IsSubscriptionEmptyAsync(adminClient, subscription, cancellationToken),
            _ => false
        };
    }

    private static async Task<bool> IsSubscriptionEmptyAsync(
        ServiceBusAdministrationClient adminClient,
        SubscriptionProperties subscription,
        CancellationToken cancellationToken)
    {
        var props = await adminClient.GetSubscriptionRuntimePropertiesAsync(subscription.TopicName, subscription.SubscriptionName, cancellationToken);
        return props.Value?.ActiveMessageCount == 0;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
