using Azure.Messaging.ServiceBus.Administration;
using MassTransit;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.MessageHub.Internal;

[ExcludeFromCodeCoverage(Justification = "Mostly MassTransit + Service Bus configuration glue")]
internal class InputStreamBuilder
{
    private readonly string _moduleName;
    private readonly MessageHubSettings _settings;
    private readonly IStreamProvider[] _providers;
    private readonly Dictionary<string, InputStream> _streams;

    public InputStreamBuilder(string moduleName, MessageHubBuilder builder, params IStreamProvider?[] providers)
    {
        StreamNameValidator.Validate(moduleName, nameof(moduleName));
        _moduleName = moduleName;
        _settings = builder.Settings;
        _providers = [.. (providers ?? []).Where(p => p != null).Cast<IStreamProvider>()];

        try
        {
            _streams = _providers.SelectMany(s => s.GetInputStreams()).ToDictionary(k => k.GetFullPath(moduleName));
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Input stream providers must have unique stream names", nameof(providers), ex);
        }
    }

    internal void RegisterInputStreamTypes(IBusRegistrationConfigurator configurator)
    {
        foreach (var stream in _streams.Values)
            configurator.AddConsumer(stream.ConsumerType);
    }

    internal void ConfigureInputStreams(IBusRegistrationContext context, IBusFactoryConfigurator configurator)
    {
        if (configurator is IServiceBusBusFactoryConfigurator serviceBusConfigurator)
        {
            foreach (var (name, stream) in _streams)
            {
                var transport = stream.Settings;

                serviceBusConfigurator.SubscriptionEndpoint<EventMessage>(name, t =>
                {
                    t.AutoDeleteOnIdle = transport.AutoDeleteOnIdle;
                    t.MaxDeliveryCount = transport.MaxDeliveryCount;
                    t.DefaultMessageTimeToLive = transport.DefaultMessageTimeToLive;
                    t.PrefetchCount = transport.PrefetchCount;
                    t.LockDuration = transport.LockDuration;
                    if (transport.MaxAutoRenewDuration.HasValue)
                        t.MaxAutoRenewDuration = transport.MaxAutoRenewDuration.Value;

                    t.ConcurrentMessageLimit = transport.ConcurrentMessagesLimit;
                    ConfigureSessionSettings(t, transport);
                    t.Rule = new CreateRuleOptions("platform_message_filter",
                        new SqlRuleFilter(StreamRoutingHelper.BuildInputFilter(_moduleName, stream)));

                    if (transport.ImmediateMessageRetryLimit.HasValue)
                        t.UseMessageRetry(x => x.Immediate(transport.ImmediateMessageRetryLimit.Value));

                    t.ConfigureConsumer(context, stream.ConsumerType);
                });
            }
        }
        else if (configurator is IInMemoryBusFactoryConfigurator inMemoryConfigurator)
        {
            // In-memory transport doesn't support topic subscriptions / SQL rule filters,
            // so we attach every stream to one receive endpoint and let
            // InMemoryMessageConsumer filter per-message.
            inMemoryConfigurator.ReceiveEndpoint(_settings.OutputStream, t =>
            {
                foreach (var (_, stream) in _streams)
                    t.Consumer(() => new InMemoryMessageConsumer(context, _moduleName, stream));
            });
        }
        else
        {
            throw new InvalidOperationException($"Unsupported bus factory configurator type {configurator.GetType()}");
        }
    }

    private static void ConfigureSessionSettings(IServiceBusSubscriptionEndpointConfigurator configurator, InputStreamSettings settings)
    {
        configurator.RequiresSession = settings.RequiresSession;
        if (settings.MaxConcurrentCallsPerSession.HasValue)
            configurator.MaxConcurrentCallsPerSession = settings.MaxConcurrentCallsPerSession.Value;

        if (settings.MaxConcurrentSessions.HasValue)
            configurator.MaxConcurrentSessions = settings.MaxConcurrentSessions.Value;

        if (settings.SessionIdleTimeout.HasValue)
            configurator.SessionIdleTimeout = settings.SessionIdleTimeout.Value;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var adminClient = new ServiceBusAdministrationClient(_settings.ConnectionString);

        var subscriptions = new List<SubscriptionProperties>();
        await foreach (var sub in adminClient.GetSubscriptionsAsync(_settings.OutputStream, cancellationToken))
            subscriptions.Add(sub);

        // Restrict to subscriptions belonging to this module.
        subscriptions = [.. subscriptions.Where(t => t.SubscriptionName.StartsWith($"{_moduleName}.", StringComparison.InvariantCultureIgnoreCase))];

        foreach (var provider in _providers)
            await CleanupForProviderAsync(adminClient, subscriptions, provider, cancellationToken);

        // Delete legacy subscriptions that don't belong to any known provider.
        var providerlessPrefix = InputStream.GetProviderlessPath(_moduleName);
        foreach (var subToDelete in subscriptions.Where(t =>
            !t.SubscriptionName.StartsWith(providerlessPrefix, StringComparison.InvariantCultureIgnoreCase)))
        {
            await adminClient.DeleteSubscriptionAsync(subToDelete.TopicName, subToDelete.SubscriptionName, cancellationToken);
        }
    }

    private async Task CleanupForProviderAsync(
        ServiceBusAdministrationClient adminClient,
        IList<SubscriptionProperties> subscriptions,
        IStreamProvider streamProvider,
        CancellationToken cancellationToken)
    {
        var lookupPrefix = InputStream.GetProviderPath(_moduleName, streamProvider.Key);

        var providerSubscriptions = subscriptions.Where(t =>
            t.SubscriptionName.StartsWith(lookupPrefix, StringComparison.InvariantCultureIgnoreCase));

        foreach (var subscriptionName in providerSubscriptions.Select(subscription => subscription.SubscriptionName))
        {
            if (_streams.ContainsKey(subscriptionName))
                continue;

            var shouldDelete = _settings.CleanupMode switch
            {
                MessageHubCleanupMode.None => false,
                MessageHubCleanupMode.DeleteAnyUnknown => true,
                MessageHubCleanupMode.DeleteEmptyUnknown =>
                    (await adminClient.GetSubscriptionRuntimePropertiesAsync(_settings.OutputStream, subscriptionName, cancellationToken)).Value?.ActiveMessageCount == 0,
                _ => false
            };

            if (shouldDelete)
                await adminClient.DeleteSubscriptionAsync(_settings.OutputStream, subscriptionName, cancellationToken);
        }
    }
}
