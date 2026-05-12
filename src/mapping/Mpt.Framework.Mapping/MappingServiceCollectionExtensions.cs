using Mpt.Framework.Mapping;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI registration extensions for the dynamic mapping engine.
/// </summary>
public static class MappingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory dynamic entity mapper. The required <c>IRqlMapAccessor</c>
    /// from <c>Mpt.Rql</c> must already be registered in the service collection (this is
    /// typically done by the standard <c>AddRql</c> setup).
    /// </summary>
    public static IServiceCollection AddInMemoryMapping(this IServiceCollection services)
    {
        services.AddScoped<InMemoryEntityMapper>();
        services.AddScoped<IDynamicEntityMapper>(sp => sp.GetRequiredService<InMemoryEntityMapper>());
        services.AddScoped<IInMemoryEntityMapper>(sp => sp.GetRequiredService<InMemoryEntityMapper>());
        return services;
    }
}
