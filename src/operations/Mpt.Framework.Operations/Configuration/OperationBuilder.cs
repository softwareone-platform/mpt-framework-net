using MassTransit;
using Mpt.Framework.Operations.Activities;
using Mpt.Framework.Operations.Models;
using Mpt.Framework.Operations.Models.Messages;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operations.Configuration;

internal interface IOperationBuilder
{
    OperationDescriptor Descriptor { get; }

    abstract void RegisterStateMachine(IBusRegistrationConfigurator<IOperationsBus> busConfigurator, OperationSettings settings, IOperationsPersistenceProvider persistence);

    abstract void RegisterEndpoints<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> busConfigurator, OperationSettings settings)
        where T : IReceiveEndpointConfigurator;
}

[SuppressMessage("Maintainability", "S2436", Justification = "Three params are needed")]
[ExcludeFromCodeCoverage(Justification = "Configuration")]
internal class OperationBuilder<TOperation, TTask, TSaga>(OperationDescriptor descriptor) : IOperationBuilder
    where TOperation : IOperationContract
    where TSaga : OperationSaga
    where TTask : class
{
    public OperationDescriptor Descriptor => descriptor;

    public void RegisterStateMachine(IBusRegistrationConfigurator<IOperationsBus> busConfigurator, OperationSettings settings, IOperationsPersistenceProvider persistence)
    {
        var stateMachine = busConfigurator.AddSagaStateMachine<OperationExecutor<TOperation, TTask, TSaga>, TSaga>();

        persistence.ConfigureSagaRepository(stateMachine);

        busConfigurator.AddConsumer<TaskEventConsumer<TOperation>>();
    }

    public void RegisterEndpoints<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> busConfigurator, OperationSettings settings)
        where T : IReceiveEndpointConfigurator
    {
        if (settings.Mode == OperationsMode.Dispatch)
            return;

        if (busConfigurator is IServiceBusBusFactoryConfigurator sbConfigurator)
            RegisterServiceBusEndpoints(context, sbConfigurator);
        else
            RegisterInMemoryEndpoints(context, busConfigurator);
    }

    [ExcludeFromCodeCoverage(Justification = "SB config")]
    private void RegisterServiceBusEndpoints(IBusRegistrationContext context, IServiceBusBusFactoryConfigurator busConfigurator)
    {
        busConfigurator.SubscriptionEndpoint(Descriptor.GetTargetName(MessageGroup.Main), Descriptor.TopicName, t =>
        {
            t.Rule = RoutingHelper.BuildFilter(Descriptor, MessageGroup.Main);
            t.RequiresSession = true;
            t.MaxConcurrentCallsPerSession = 1;

            ConfigureGroup(context, t, MessageGroup.Main);
            ApplyEndpontOptions(t, Descriptor.Processing.Main);
            t.RethrowFaultedMessages();
        });

        busConfigurator.SubscriptionEndpoint(Descriptor.GetTargetName(MessageGroup.Events), Descriptor.TopicName, t =>
        {
            t.Rule = RoutingHelper.BuildFilter(Descriptor, MessageGroup.Events);

            ConfigureGroup(context, t, MessageGroup.Events);

            t.LockDuration = TimeSpan.FromMinutes(1);
            t.MaxDeliveryCount = 3;
            t.RethrowFaultedMessages();
        });

        busConfigurator.SubscriptionEndpoint(Descriptor.GetTargetName(MessageGroup.Tasks), Descriptor.TopicName, t =>
        {
            t.Rule = RoutingHelper.BuildFilter(Descriptor, MessageGroup.Tasks);
            ConfigureGroup(context, t, MessageGroup.Tasks);
            ApplyEndpontOptions(t, Descriptor.Processing.Tasks);
            t.RethrowFaultedMessages();
        });
    }

    public void RegisterInMemoryEndpoints(IBusRegistrationContext context, IBusFactoryConfigurator busConfigurator)
    {
        busConfigurator.ReceiveEndpoint(Descriptor.GetQueueName(MessageGroup.Main), t =>
        {
            ConfigureGroup(context, t, MessageGroup.Main);
            t.ConfigureConsumeTopology = false;
            // This is a workaround for the in-memory transport (for tests), which does not support sessions
            t.ConcurrentMessageLimit = 1;
        });

        busConfigurator.ReceiveEndpoint(Descriptor.GetQueueName(MessageGroup.Events), t =>
        {
            ConfigureGroup(context, t, MessageGroup.Events);
            t.ConfigureConsumeTopology = false;
        });

        busConfigurator.ReceiveEndpoint(Descriptor.GetQueueName(MessageGroup.Tasks), t =>
        {
            ConfigureGroup(context, t, MessageGroup.Tasks);
            t.ConfigureConsumeTopology = false;
        });
    }

    private void ConfigureGroup(IBusRegistrationContext context, IReceiveEndpointConfigurator configurator, MessageGroup group)
    {
        switch (group)
        {
            case MessageGroup.Main:
                configurator.ConfigureSaga<TSaga>(context);
                break;
            case MessageGroup.Tasks:
                configurator.ConfigureSaga<TSaga>(context);
                configurator.ConfigureMessageTopology<TTask>();
                break;
            case MessageGroup.Events:
                configurator.PrefetchCount = 256;
                configurator.Batch<TaskCompletedMessage>(batchConfig =>
                {
                    batchConfig.MessageLimit = 100;
                    batchConfig.TimeLimit = TimeSpan.FromSeconds(3);
                });
                configurator.ConfigureConsumer<TaskEventConsumer<TOperation>>(context);
                break;
            default:
                break;
        }
    }

    private static void ApplyEndpontOptions(IServiceBusSubscriptionEndpointConfigurator cfg, GroupProcessingOptions options)
    {
        cfg.ConcurrentMessageLimit = options.Concurrency;
        cfg.MaxConcurrentSessions = options.Concurrency;
        cfg.PrefetchCount = options.PrefetchCount;
        cfg.LockDuration = options.MinProcessingTime;
        cfg.MaxDeliveryCount = options.MaxAttempts;
        cfg.MaxAutoRenewDuration = options.MaxProcessingTime;

        if (options.Retry != null)
        {
            var retry = options.Retry;
            cfg.UseMessageRetry(r =>
            {
                r.Exponential(retry.RetryLimit, retry.MinInterval, retry.MaxInterval, retry.IntervalDelta);
                r.Handle(retry.Filter);
            });

            var maxTime = retry.CalculateMaxDelay();
            if (options.MaxProcessingTime < maxTime)
            {
                cfg.MaxAutoRenewDuration = maxTime;
            }
        }
    }
}
