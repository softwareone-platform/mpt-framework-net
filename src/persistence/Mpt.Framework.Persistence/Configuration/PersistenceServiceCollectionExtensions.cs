using Mpt.Framework.Persistence;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI registration extensions for the Mpt.Framework.Persistence engine.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the persistence engine — <see cref="IUnitOfWork"/>, open-generic defaults
    /// for entity configurations / lifecycle hooks / event producers, and invokes the
    /// supplied configure callback to wire up entity slices and a persistence flavour.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="moduleCode">Caller-defined module code (used by event-producing code that wants per-module routing keys).</param>
    /// <param name="configure">Callback to register entities and pick a persistence flavour (e.g. <c>AddEfCorePersistence&lt;TDbContext&gt;</c>).</param>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string moduleCode,
        Action<PersistenceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(moduleCode);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        var builder = new PersistenceBuilder(services, moduleCode);
        builder.AddDefaultEntityServices();
        configure(builder);

        return services;
    }
}
