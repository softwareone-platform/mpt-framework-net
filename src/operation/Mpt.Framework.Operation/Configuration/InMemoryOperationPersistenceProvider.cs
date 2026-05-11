using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.Operation.Configuration;

internal sealed class InMemoryOperationPersistenceProvider : IOperationPersistenceProvider
{
    public void RegisterServices(IServiceCollection services, OperationSettings settings, IReadOnlyCollection<OperationSagaRegistration> registrations)
    {
    }

    public void ConfigureSagaRepository<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.InMemoryRepository();
    }

    public void ConfigureBus<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> configurator, OperationSettings settings)
        where T : IReceiveEndpointConfigurator
    {
    }
}
