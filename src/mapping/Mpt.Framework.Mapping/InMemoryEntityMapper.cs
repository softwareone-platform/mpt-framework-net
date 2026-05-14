using Mpt.Rql;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mpt.Framework.Mapping;

/// <summary>
/// In-memory implementation of <see cref="DynamicEntityMapper"/>. References are walked
/// recursively, platform-object collections are processed in place, and there are no
/// persistence side effects.
/// </summary>
public class InMemoryEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
    : DynamicEntityMapper(serviceProvider, rqlMapAccessor), IInMemoryEntityMapper
{
    /// <inheritdoc />
    protected internal override bool UseAssignForPlatformEntities => false;

    /// <inheritdoc />
    protected internal override Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty)
        => Task.FromResult(true);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Not reachable through this mapper: UseAssignForPlatformEntities is false so MappingExecutor never invokes FindEntityAsync. The override exists to honor the abstract contract.")]
    protected internal override Task<object?> FindEntityAsync(Type entityType, object entity)
        => Task.FromResult<object?>(null);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Not reachable through this mapper: UseAssignForPlatformEntities is false so MappingExecutor walks platform references in place and never invokes UpdatePlatformEntityReference.")]
    protected internal override Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity)
        => Task.FromResult(0);
}
