using System.Linq.Expressions;

namespace Mpt.Framework.Mapping;

/// <summary>
/// Maps the values of a view-model into an existing persistence entity in place,
/// counting how many properties changed.
/// </summary>
/// <remarks>
/// Implementations drive the mapping from RQL metadata so that nested objects and
/// collections are walked in the same shape the projection query produced.
/// </remarks>
public interface IDynamicEntityMapper
{
    /// <summary>
    /// Maps only the primitive (non-reference, non-collection) properties of <paramref name="from"/>
    /// onto <paramref name="to"/>.
    /// </summary>
    /// <typeparam name="TDbEntity">The type of the persistence entity.</typeparam>
    /// <typeparam name="TEntity">The type of the source view-model.</typeparam>
    /// <param name="from">The source view-model carrying the new values.</param>
    /// <param name="to">The persistence entity to update in place.</param>
    /// <returns>The number of properties that were changed.</returns>
    Task<int> MapPrimitiveAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to);

    /// <summary>
    /// Maps only the reference and collection (complex) properties of <paramref name="from"/>
    /// onto <paramref name="to"/>.
    /// </summary>
    /// <typeparam name="TDbEntity">The type of the persistence entity.</typeparam>
    /// <typeparam name="TEntity">The type of the source view-model.</typeparam>
    /// <param name="from">The source view-model carrying the new values.</param>
    /// <param name="to">The persistence entity to update in place.</param>
    /// <returns>The number of properties that were changed.</returns>
    Task<int> MapComplexAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to);

    /// <summary>
    /// Maps all mappable properties (primitive and complex) of <paramref name="from"/>
    /// onto <paramref name="to"/>.
    /// </summary>
    /// <typeparam name="TDbEntity">The type of the persistence entity.</typeparam>
    /// <typeparam name="TEntity">The type of the source view-model.</typeparam>
    /// <param name="from">The source view-model carrying the new values.</param>
    /// <param name="to">The persistence entity to update in place.</param>
    /// <returns>The number of properties that were changed.</returns>
    Task<int> MapAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to);

    /// <summary>
    /// Maps the single property identified by <paramref name="path"/> from <paramref name="from"/>
    /// onto <paramref name="to"/>, recursively mapping nested objects on that path.
    /// </summary>
    /// <typeparam name="TDbEntity">The type of the persistence entity.</typeparam>
    /// <typeparam name="TEntity">The type of the source view-model.</typeparam>
    /// <param name="from">The source view-model carrying the new value.</param>
    /// <param name="path">An expression selecting a property (or property chain) on the source.</param>
    /// <param name="to">The persistence entity to update in place.</param>
    /// <returns>The number of properties that were changed.</returns>
    Task<int> MapPathAsync<TDbEntity, TEntity>(TEntity from, Expression<Func<TEntity, object>> path, TDbEntity to);
}
