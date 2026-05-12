namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic marker for filter providers — exists so the engine can locate providers
/// by interface in DI.
/// </summary>
public interface IFilterProvider;

/// <summary>
/// Extensibility hook for translating <see cref="CustomFilters"/> from a query request
/// into <see cref="IQueryable{TDbModel}"/> filters on the persistence-side entity.
/// </summary>
/// <typeparam name="TDbModel">The persistence-side entity type.</typeparam>
public interface IFilterProvider<TDbModel> : IFilterProvider where TDbModel : class
{
    /// <summary>
    /// Apply each filter in <paramref name="filtersParams"/> to <paramref name="query"/>,
    /// returning the resulting query.
    /// </summary>
    IQueryable<TDbModel> Apply(CustomFilters filtersParams, IQueryable<TDbModel> query);
}
