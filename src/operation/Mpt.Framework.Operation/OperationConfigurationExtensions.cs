using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operation.Communication;
using Mpt.Framework.Operation.Configuration;
using Mpt.Framework.Operation.Maintenance;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation;

[ExcludeFromCodeCoverage]
public static class OperationConfigurationExtensions
{
    public static IServiceCollection AddOperation(this IServiceCollection services, string moduleCode, Action<OperationBuilder> configure)
        => services.AddOperation(moduleCode, new OperationSettings(), configure);

    public static IServiceCollection AddOperation(this IServiceCollection services, string moduleCode, OperationSettings settings, Action<OperationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OperationBuilder(services, moduleCode, settings);

        configure(builder);

        services.AddSingleton(settings);
        services.AddScoped(typeof(IOperationMessageSender<>), typeof(OperationMessageSender<>));
        services.AddScoped<IOperationDispatcher, OperationDispatcher>();

        if (settings.Cleanup != OperationCleanupMode.None)
            services.AddHostedService<OperationCleanupService>();

        services.AddSingleton<IOperationProvider>(new OperationProvider(builder.Descriptors));

        var persistence = builder.Persistence;
        var sagaRegistrations = builder.Descriptors.Values
            .Select(d => new OperationSagaRegistration(d.SagaType, d.Name))
            .ToList();
        persistence.RegisterServices(services, settings, sagaRegistrations);

        // Prepare per-operation registrations if consumption is enabled
        var registrations = settings.Mode == OperationMode.ConsumeAndDispatch ? GetRegistrations(builder.Descriptors).ToList() : [];

        services.AddMassTransit<IOperationBus>(busBuilder =>
        {
            foreach (var registration in registrations)
            {
                registration.RegisterStateMachine(busBuilder, settings, persistence);
            }

            switch (settings.Transport)
            {
                case OperationTransport.InMemory:
                    busBuilder.UsingInMemory((context, cfg) =>
                    {
                        ConfigureTransport(registrations, context, cfg, settings, persistence);
                    });
                    break;
                case OperationTransport.ServiceBus:
                    busBuilder.UsingAzureServiceBus((context, cfg) =>
                    {
                        cfg.Host(settings.ConnectionString);
                        ConfigureTransport(registrations, context, cfg, settings, persistence);
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported transport type {settings.Transport}");
            }
        });

        return services;
    }

    private static void ConfigureTransport<T>(List<IOperationRegistration> registrations, IBusRegistrationContext context, IBusFactoryConfigurator<T> cfg, OperationSettings settings, IOperationPersistenceProvider persistence)
        where T : IReceiveEndpointConfigurator
    {
        persistence.ConfigureBus(context, cfg, settings);

        cfg.UseMessageScope(context);

        cfg.ConfigureJsonSerializerOptions(options =>
        {
            OperationSerializerOptions.ConfigureDefaultOptions(options);
            return options;
        });

        if (settings.Mode == OperationMode.ConsumeAndDispatch)
        {
            foreach (var registration in registrations)
            {
                registration.RegisterEndpoints(context, cfg, settings);
            }

            cfg.ConfigureEndpoints(context);
        }
    }

    private static IEnumerable<IOperationRegistration> GetRegistrations(IReadOnlyDictionary<Type, OperationDescriptor> descriptors)
    {
        foreach (var desc in descriptors.Values)
        {
            var registrationType = typeof(OperationRegistration<,,>).MakeGenericType(desc.OperationType, desc.TaskType, desc.SagaType);
            yield return (IOperationRegistration)Activator.CreateInstance(registrationType, desc)!;
        }
    }
}
