using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mpt.Framework.Persistence;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Persistence.Tests.Fixtures;

/// <summary>
/// Spin-up helper that builds a service provider with EF Core (InMemory), RQL,
/// the dynamic mapper, and the Persistence engine — all wired against the local
/// widget fixture types.
/// </summary>
public static class PersistenceFixture
{
    public static ServiceProvider Build(
        Action<PersistenceBuilder>? extraConfigure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<WidgetDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRql(c => c.ScanForMappers(typeof(WidgetMap).Assembly));
        services.AddInMemoryMapping();
        services.AddEfCoreMapping<WidgetDbContext>();

        services.AddPersistence("test-module", builder =>
        {
            builder.AddEfCorePersistence<WidgetDbContext>();
            builder.AddEntity<WidgetDbEntity, WidgetView, WidgetQueryService>();
            builder.ScanForConfigurations(Assembly.GetExecutingAssembly());

            extraConfigure?.Invoke(builder);
        });

        // Run after AddPersistence so caller overrides win (Microsoft DI is last-wins).
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
