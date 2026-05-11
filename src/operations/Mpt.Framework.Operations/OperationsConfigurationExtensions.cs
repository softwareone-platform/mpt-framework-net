using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operations.Communication;
using Mpt.Framework.Operations.Configuration;
using Mpt.Framework.Operations.Maintenance;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operations;

[ExcludeFromCodeCoverage]
public static class OperationsConfigurationExtensions
{
    public static IServiceCollection AddOperations(this IServiceCollection services, string moduleCode, Action<OperationsBuilder> configure)
        => services.AddOperations(moduleCode, new OperationSettings(), configure);

    public static IServiceCollection AddOperations(this IServiceCollection services, string moduleCode, OperationSettings settings, Action<OperationsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OperationsBuilder(services, moduleCode, settings);

        configure(builder);

        services.AddSingleton(settings);
        services.AddScoped(typeof(IOperationMessageSender<>), typeof(OperationMessageSender<>));
        services.AddScoped<IOperationDispatcher, OperationDispatcher>();

        if (settings.Cleanup != OperationsCleanupMode.None)
            services.AddHostedService<OperationsCleanupService>();

        services.AddSingleton<IOperationProvider>(new OperationProvider(builder.Descriptors));

        var persistence = builder.Persistence;
        var sagaRegistrations = builder.Descriptors.Values
            .Select(d => new OperationSagaRegistration(d.SagaType, d.Name))
            .ToList();
        persistence.RegisterServices(services, settings, sagaRegistrations);

        // Prepare operation builders if consumption is enabled
        var builders = settings.Mode == OperationsMode.ConsumeAndDispatch ? GetBuilders(builder.Descriptors).ToList() : [];

        services.AddMassTransit<IOperationsBus>(busBuilder =>
        {
            foreach (var config in builders)
            {
                config.RegisterStateMachine(busBuilder, settings, persistence);
            }

            switch (settings.Transport)
            {
                case OperationsTransport.InMemory:
                    busBuilder.UsingInMemory((context, cfg) =>
                    {
                        ConfigureTransport(builders, context, cfg, settings, persistence);
                    });
                    break;
                case OperationsTransport.ServiceBus:
                    busBuilder.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(settings.ConnectionString);
                        ConfigureTransport(builders, context, cfg, settings, persistence);
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported transport type {settings.Transport}");
            }
        });

        return services;
    }

    private static void ConfigureTransport<T>(List<IOperationBuilder> builders, IBusRegistrationContext context, IBusFactoryConfigurator<T> cfg, OperationSettings settings, IOperationsPersistenceProvider persistence)
        where T : IReceiveEndpointConfigurator
    {
        persistence.ConfigureBus(context, cfg, settings);

        cfg.UseMessageScope(context);

        cfg.ConfigureJsonSerializerOptions(options =>
        {
            OperationSerializerOptions.ConfigureDefaultOptions(options);
            return options;
        });

        if (settings.Mode == OperationsMode.ConsumeAndDispatch)
        {
            foreach (var config in builders)
            {
                config.RegisterEndpoints(context, cfg, settings);
            }

            cfg.ConfigureEndpoints(context);
        }
    }

    private static IEnumerable<IOperationBuilder> GetBuilders(IReadOnlyDictionary<Type, OperationDescriptor> descriptors)
    {
        foreach (var desc in descriptors.Values)
        {
            var configType = typeof(OperationBuilder<,,>).MakeGenericType(desc.OperationType, desc.TaskType, desc.SagaType);
            yield return (IOperationBuilder)Activator.CreateInstance(configType, desc)!;
        }
    }
}
