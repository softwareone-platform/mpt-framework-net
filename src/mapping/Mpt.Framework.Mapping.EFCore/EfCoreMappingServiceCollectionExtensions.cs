using Microsoft.EntityFrameworkCore;
using Mpt.Framework.Mapping;
using Mpt.Rql;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI registration extensions for the EF Core flavour of the dynamic mapping engine.
/// </summary>
public static class EfCoreMappingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EF Core dynamic entity mapper backed by the supplied
    /// <typeparamref name="TDbContext"/>. The DbContext, an <c>IRqlMapAccessor</c> (from
    /// the standard <c>AddRql</c> setup), and an <c>IServiceProvider</c> must already be
    /// resolvable from the container.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type to query through.</typeparam>
    public static IServiceCollection AddEfCoreMapping<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped(sp => new EfCoreDynamicEntityMapper(
            sp,
            sp.GetRequiredService<IRqlMapAccessor>(),
            sp.GetRequiredService<TDbContext>()));
        services.AddScoped<IDynamicEntityMapper>(sp => sp.GetRequiredService<EfCoreDynamicEntityMapper>());
        services.AddScoped<IEfCoreDynamicEntityMapper>(sp => sp.GetRequiredService<EfCoreDynamicEntityMapper>());
        return services;
    }
}
