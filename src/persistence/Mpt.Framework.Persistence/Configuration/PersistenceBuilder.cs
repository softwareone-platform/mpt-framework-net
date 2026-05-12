using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Fluent builder returned by <c>services.AddPersistence(...)</c>. Use it to register
/// per-entity slices (repository + query service), declare which assemblies hold
/// configurations / hooks / producers, and plug in the persistence flavour (the
/// in-memory default is registered automatically; the EFCore add-on overrides it via
/// <c>builder.AddEfCorePersistence&lt;TDbContext&gt;()</c>).
/// </summary>
public class PersistenceBuilder(IServiceCollection services, string moduleCode)
{
    /// <summary>The DI container being configured.</summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    /// Caller-supplied module code — passed through to anything that needs to scope its
    /// configuration by module (e.g. routing keys when MessageHub is wired up).
    /// </summary>
    public string ModuleCode { get; } = moduleCode;

    /// <summary>
    /// Resolver invoked by per-entity registration (<see cref="AddEntity{TDbEntity, TEntity, TQueryService}"/>)
    /// to determine which concrete <c>Repository&lt;TEntity&gt;</c> subclass to register.
    /// Defaults to the in-memory repository; the EFCore add-on swaps in <c>EfCoreRepository&lt;,&gt;</c>.
    /// </summary>
    public Func<Type, Type, Type> RepositoryTypeResolver { get; set; } = static (_, _) =>
        throw new InvalidOperationException(
            "No persistence flavour registered. Call AddEfCorePersistence<TDbContext>() inside AddPersistence(...) " +
            "for SQL Server, or register an in-memory repository for tests.");

    /// <summary>
    /// Registers a per-entity slice: (TDbEntity, TEntity) plus the user-supplied
    /// <typeparamref name="TQueryService"/> subclass. The configured
    /// <see cref="RepositoryTypeResolver"/> picks the repository implementation.
    /// </summary>
    public PersistenceBuilder AddEntity<TDbEntity, TEntity, TQueryService>()
        where TDbEntity : class
        where TEntity : class, IPlatformEntity, IRqlGraphHolder, new()
        where TQueryService : QueryService<TDbEntity, TEntity>
    {
        Services.AddScoped<IQueryService<TEntity>, TQueryService>();
        Services.AddScoped(typeof(IRepository<TEntity>), RepositoryTypeResolver(typeof(TDbEntity), typeof(TEntity)));
        Services.AddScoped<IFilterProvider<TDbEntity>, DefaultFilterProvider<TDbEntity>>();
        return this;
    }

    /// <summary>
    /// Scans <paramref name="assembly"/> for <see cref="IEntityConfiguration"/> /
    /// <see cref="IEntityLifecycleHooks"/> / <see cref="IEntityEventProducer"/> implementations
    /// and registers each against its strongly-typed open-generic interface.
    /// </summary>
    public PersistenceBuilder ScanForConfigurations(Assembly assembly)
    {
        var candidates = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IEntityConfiguration).IsAssignableFrom(t)
                     || typeof(IEntityLifecycleHooks).IsAssignableFrom(t)
                     || typeof(IEntityEventProducer).IsAssignableFrom(t))
            .ToArray();

        RegisterClosedGenerics(candidates, typeof(IEntityConfiguration<>), singleton: true);
        RegisterClosedGenerics(candidates, typeof(IEntityLifecycleHooks<>), singleton: false);
        RegisterClosedGenerics(candidates, typeof(IEntityEventProducer<>), singleton: false);

        return this;
    }

    /// <summary>
    /// Registers the default open-generic implementations for entity types the caller
    /// hasn't supplied a custom configuration / hooks / producer for.
    /// </summary>
    /// <remarks>
    /// Call once, typically before <see cref="ScanForConfigurations(Assembly)"/>, so the
    /// scan can override the defaults for entities that do have custom implementations.
    /// </remarks>
    public PersistenceBuilder AddDefaultEntityServices()
    {
        Services.AddSingleton(typeof(IEntityConfiguration<>), typeof(EntityConfiguration<>));
        Services.AddScoped(typeof(IEntityLifecycleHooks<>), typeof(EntityLifecycleHooks<>));
        Services.AddScoped(typeof(IEntityEventProducer<>), typeof(EntityEventProducer<>));
        return this;
    }

    private void RegisterClosedGenerics(Type[] candidates, Type openGenericInterface, bool singleton)
    {
        var alreadyRegistered = new HashSet<Type>();

        foreach (var type in candidates)
        {
            var ifaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

            foreach (var iface in ifaces)
            {
                var entityType = iface.GetGenericArguments()[0];
                if (!alreadyRegistered.Add(entityType))
                    continue;

                var descriptor = singleton
                    ? ServiceDescriptor.Singleton(iface, type)
                    : ServiceDescriptor.Scoped(iface, type);

                Services.Add(descriptor);
            }
        }
    }
}
