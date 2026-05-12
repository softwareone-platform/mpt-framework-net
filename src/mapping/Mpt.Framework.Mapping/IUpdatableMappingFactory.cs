namespace Mpt.Framework.Mapping;

/// <summary>
/// Hook implemented by factories that participate in the dynamic mapper's update pass.
/// </summary>
/// <remarks>
/// When <c>Mpt.Rql</c> wires a property mapping to a factory type (typically via
/// <c>IRqlMapperContext.MapWithFactory</c>) and that factory implements this interface,
/// <see cref="DynamicEntityMapper"/> resolves the factory from DI and delegates the
/// property update to <see cref="TryUpdate"/> instead of walking the property generically.
/// Use this for computed properties, aggregations, or anything where the update path
/// needs custom logic beyond a straight copy.
/// </remarks>
public interface IUpdatableMappingFactory
{
    /// <summary>
    /// Updates <paramref name="entity"/> using the value from <paramref name="input"/>,
    /// signalling whether anything actually changed.
    /// </summary>
    /// <param name="input">The property value from the source view-model (may be <see langword="null"/>).</param>
    /// <param name="entity">The persistence entity being updated.</param>
    /// <param name="hasChanges">Set to <see langword="true"/> if the call mutated <paramref name="entity"/>.</param>
    void TryUpdate(object? input, object entity, out bool hasChanges);
}
