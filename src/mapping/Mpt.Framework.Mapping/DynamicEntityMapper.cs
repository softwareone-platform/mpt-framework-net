using Mpt.Rql;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Framework.Mapping;

/// <summary>
/// Abstract base for a dynamic, RQL-aware entity mapper that updates persistence entities
/// in place from view-model instances. Subclass and supply the persistence-specific
/// extension points to plug in a database backend; see <see cref="InMemoryEntityMapper"/>
/// for the in-memory default.
/// </summary>
public abstract class DynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor) : IDynamicEntityMapper
{
    internal IServiceProvider ServiceProvider => serviceProvider;

    /// <inheritdoc />
    public async Task<int> MapPrimitiveAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to)
    {
        var executor = new MappingExecutor(rqlMapAccessor, this);
        return await executor.MapInternalAsync(typeof(TEntity), from!, to!, MapModes.Primitive, null);
    }

    /// <inheritdoc />
    public async Task<int> MapComplexAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to)
    {
        var executor = new MappingExecutor(rqlMapAccessor, this);
        return await executor.MapInternalAsync(typeof(TEntity), from!, to!, MapModes.Complex, null);
    }

    /// <inheritdoc />
    public async Task<int> MapAsync<TDbEntity, TEntity>(TEntity from, TDbEntity to)
    {
        var executor = new MappingExecutor(rqlMapAccessor, this);
        return await executor.MapInternalAsync(typeof(TEntity), from!, to!, MapModes.All, null);
    }

    /// <inheritdoc />
    public async Task<int> MapPathAsync<TDbEntity, TEntity>(TEntity from, Expression<Func<TEntity, object>> path, TDbEntity to)
    {
        var executor = new MappingExecutor(rqlMapAccessor, this, path);
        return await executor.MapInternalAsync(typeof(TEntity), from!, to!, MapModes.All, null);
    }

    /// <summary>
    /// When <see langword="true"/>, platform-entity references are reassigned by id
    /// (via <see cref="UpdatePlatformEntityReference"/>) instead of having their
    /// properties walked and copied.
    /// </summary>
    protected internal abstract bool UseAssignForPlatformEntities { get; }

    /// <summary>
    /// Looks up an existing persistence-side instance for the given source platform object.
    /// Implementations typically query the database by id.
    /// </summary>
    protected internal abstract Task<object?> FindEntityAsync(Type entityType, object entity);

    /// <summary>
    /// Ensures the supplied collection navigation is loaded (e.g. EF Core's <c>EnsureCollection</c>),
    /// returning <see langword="true"/> when the collection is now ready to be mutated.
    /// </summary>
    protected internal abstract Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty);

    /// <summary>
    /// Hook invoked when the mapper removes a platform object from an identifiable collection.
    /// </summary>
    protected internal virtual Task EnsureEntityRemovedAsync(object entity) => Task.CompletedTask;

    /// <summary>
    /// Reassigns a reference-typed platform-entity property to the given source entity,
    /// returning the number of properties that changed (0 or 1).
    /// </summary>
    protected internal abstract Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity);

    /// <summary>
    /// Selects which categories of properties a mapping pass should touch.
    /// </summary>
    [Flags]
    public enum MapModes
    {
        /// <summary>Map nothing.</summary>
        None = 0,
        /// <summary>Map primitive (non-reference, non-collection) properties only.</summary>
        Primitive = 1 << 0,
        /// <summary>Map reference and collection (complex) properties only.</summary>
        Complex = 1 << 1,
        /// <summary>Map all mappable properties.</summary>
        All = Primitive | Complex,
    }
}
