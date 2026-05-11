using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.EFCore;

internal sealed class SqlServerPersistenceProvider(string connectionString) : IOperationPersistenceProvider
{
    private const int SqlTimeoutErrorCode = -2;

    public void RegisterServices(IServiceCollection services, OperationSettings settings, IReadOnlyCollection<OperationSagaRegistration> registrations)
    {
        if (settings.Mode != OperationMode.ConsumeAndDispatch)
            return;

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("ConnectionString must be provided when using SQL Server persistence");

        services.AddDbContext<OperationDbContext>(t =>
        {
            t.UseSqlServer(connectionString, c =>
            {
                c.EnableRetryOnFailure(2, TimeSpan.FromMilliseconds(100), [SqlTimeoutErrorCode]);
            });
        });

        var children = registrations.Select(r => (r.SagaType, r.OperationName)).ToList();
        services.AddSingleton(new OperationSagaTypes(children));
    }

    public void ConfigureSagaRepository<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OperationDbContext>();
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.IsolationLevel = System.Data.IsolationLevel.Snapshot;
        });
    }

    public void ConfigureBus<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> configurator, OperationSettings settings)
        where T : IReceiveEndpointConfigurator
    {
    }
}
