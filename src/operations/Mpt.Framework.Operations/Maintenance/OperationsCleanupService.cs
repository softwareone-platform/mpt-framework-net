using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using Mpt.Framework.Operations.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operations.Maintenance;

[ExcludeFromCodeCoverage]
internal class OperationsCleanupService(OperationSettings configuration, IOperationProvider operationProvider) : IHostedService
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
        List<OperationDescriptor> builders,
        CancellationToken cancellationToken)
    {
        await foreach (var subscription in adminClient.GetSubscriptionsAsync(topicName, cancellationToken: cancellationToken))
        {
            if (IsKnownSubscription(subscription.SubscriptionName, builders))
                continue;

            if (await ShouldDeleteSubscriptionAsync(adminClient, subscription, cancellationToken))
            {
                await adminClient.DeleteSubscriptionAsync(subscription.TopicName, subscription.SubscriptionName, cancellationToken);
            }
        }
    }

    private bool ShouldRunCleanup()
    {
        return configuration.Cleanup != OperationsCleanupMode.None
            && configuration.ConnectionString != null
            && configuration.Transport == OperationsTransport.ServiceBus;
    }

    private static bool IsKnownSubscription(string subscriptionName, List<OperationDescriptor> builders)
    {
        return builders.Exists(t => subscriptionName.StartsWith(t.Name));
    }

    private async Task<bool> ShouldDeleteSubscriptionAsync(
        ServiceBusAdministrationClient adminClient,
        SubscriptionProperties subscription,
        CancellationToken cancellationToken)
    {
        return configuration.Cleanup switch
        {
            OperationsCleanupMode.DeleteAnyUnknown => true,
            OperationsCleanupMode.DeleteEmptyUnknown => await IsSubscriptionEmptyAsync(adminClient, subscription, cancellationToken),
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
