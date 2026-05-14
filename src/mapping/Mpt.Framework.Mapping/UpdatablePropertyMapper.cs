using Mpt.Rql;
using System.Linq.Expressions;

namespace Mpt.Framework.Mapping;

/// <summary>
/// Abstract base class for custom mapping factories that participate in both query projection
/// (via <see cref="IRqlMappingExpressionFactory{TStorage}"/>) and the dynamic mapper's update
/// pass (via <see cref="IUpdatableMappingFactory"/>).
/// </summary>
/// <typeparam name="TStorage">The persistence entity type.</typeparam>
/// <typeparam name="TInput">The input property type expected from the source view-model.</typeparam>
/// <remarks>
/// Use this when straight RQL property mapping is insufficient — computed properties,
/// aggregations of multiple source values, or custom transformation logic. Factories must
/// be registered in DI so the mapper can resolve them at runtime. The <typeparamref name="TInput"/>
/// parameter is a compile-time check that the factory is wired to a property of the right type.
/// </remarks>
public abstract class UpdatablePropertyMapper<TStorage, TInput> : IUpdatableMappingFactory, IRqlMappingExpressionFactory<TStorage>
{
    /// <summary>
    /// Returns the expression used for query projection from storage to view.
    /// </summary>
    public abstract Expression<Func<TStorage, object?>> GetStorageExpression();

    /// <summary>
    /// Hint that controls how the dynamic mapper uses this factory during expression generation.
    /// </summary>
    public virtual ExpressionFactoryHint Hint => ExpressionFactoryHint.None;

    /// <summary>
    /// Updates the storage entity with values derived from <paramref name="input"/> and
    /// signals whether anything actually changed via <paramref name="hasChanges"/>.
    /// </summary>
    public abstract void TryUpdate(TInput? input, TStorage entity, out bool hasChanges);

    void IUpdatableMappingFactory.TryUpdate(object? input, object entity, out bool hasChanges)
    {
        if (entity is not TStorage storageEntity)
            throw new ArgumentException($"Invalid entity type. Expected {typeof(TStorage).Name}, got {entity?.GetType().Name ?? "null"}.");

        TryUpdate((TInput?)input, storageEntity, out hasChanges);
    }
}
