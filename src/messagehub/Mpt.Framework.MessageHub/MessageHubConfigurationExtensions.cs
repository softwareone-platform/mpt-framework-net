using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.MessageHub.Internal;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.MessageHub;

[ExcludeFromCodeCoverage(Justification = "Composition root")]
public static class MessageHubConfigurationExtensions
{
    public static IServiceCollection AddMessageHub(this IServiceCollection services, string moduleCode, Action<MessageHubBuilder> configure)
        => services.AddMessageHub(moduleCode, new MessageHubSettings(), configure);

    public static IServiceCollection AddMessageHub(this IServiceCollection services, string moduleCode, MessageHubSettings settings, Action<MessageHubBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MessageHubBuilder(services, moduleCode, settings);
        configure(builder);

        if (settings.Transport != MessageHubTransport.InMemory && string.IsNullOrEmpty(settings.ConnectionString))
            throw new InvalidOperationException("MessageHub: ConnectionString is required for non-InMemory transports.");

        services.AddSingleton(settings);
        services.AddSingleton(builder);
        services.AddSingleton<IMessageHubPublisher, MessageHubPublisher>();

        var streamBuilder = new InputStreamBuilder(builder.ModuleCode, builder, [.. builder.StreamProviders]);

        if (settings.Transport == MessageHubTransport.ServiceBus && settings.CleanupMode != MessageHubCleanupMode.None)
            services.AddHostedService(_ => new MessageHubCleanupService(streamBuilder));

        services.AddMassTransit<IMessageHubBus>(mt =>
        {
            streamBuilder.RegisterInputStreamTypes(mt);

            switch (settings.Transport)
            {
                case MessageHubTransport.InMemory:
                    mt.UsingInMemory((context, cfg) =>
                    {
                        ConfigureTransport(context, cfg, streamBuilder);
                        cfg.DeployPublishTopology = false; // single queue for in-memory case
                    });
                    break;
                case MessageHubTransport.ServiceBus:
                    mt.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(settings.ConnectionString);

                        cfg.Message<EventMessage>(t => t.SetEntityName(settings.OutputStream));

                        cfg.DeployPublishTopology = true; // topic for SB case
                        cfg.UseServiceBusMessageScheduler();
                        ConfigureTransport(context, cfg, streamBuilder);
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported transport type {settings.Transport}");
            }
        });

        return services;
    }

    private static void ConfigureTransport<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> cfg, InputStreamBuilder streamBuilder)
        where T : IReceiveEndpointConfigurator
    {
        cfg.ConfigureJsonSerializerOptions(options =>
        {
            MessageHubSerializerOptions.ConfigureDefaultOptions(options);
            return options;
        });

        streamBuilder.ConfigureInputStreams(context, cfg);
    }
}
