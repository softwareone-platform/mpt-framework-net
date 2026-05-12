using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mpt.Framework.Persistence;
using Mpt.Framework.Persistence.EFCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI registration extensions for the EF Core flavour of Mpt.Framework.Persistence.
/// Call <see cref="AddEfCorePersistence{TDbContext}"/> on the <see cref="PersistenceBuilder"/>
/// supplied to <c>services.AddPersistence(...)</c>.
/// </summary>
public static class EfCorePersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Wires the persistence engine to use EF Core against <typeparamref name="TDbContext"/>:
    /// substitutes the EF Core async query execution strategy, the EF-flavour unit of
    /// work, and configures the builder so future <c>AddEntity&lt;TDbEntity, TEntity, …&gt;</c>
    /// calls register <see cref="EfCoreRepository{TDbEntity, TEntity}"/> as the
    /// implementation.
    /// </summary>
    /// <typeparam name="TDbContext">The consumer-supplied EF Core <see cref="DbContext"/>.</typeparam>
    public static PersistenceBuilder AddEfCorePersistence<TDbContext>(this PersistenceBuilder builder)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        builder.Services.AddScoped<IQueryExecutionStrategy>(_ => EfCoreQueryExecutionStrategy.Instance);
        builder.Services.AddScoped<IUnitOfWork>(sp => new EfCoreUnitOfWork(
            sp,
            sp.GetRequiredService<ILogger<UnitOfWork>>(),
            sp.GetRequiredService<TDbContext>()));

        builder.RepositoryTypeResolver = static (dbEntity, entity)
            => typeof(EfCoreRepository<,>).MakeGenericType(dbEntity, entity);

        return builder;
    }
}
