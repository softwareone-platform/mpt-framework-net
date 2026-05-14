using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mpt.Rql;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mpt.Framework.Mapping;

/// <summary>
/// EF Core flavoured implementation of <see cref="DynamicEntityMapper"/>. References to
/// <see cref="IPlatformObject"/> properties are reassigned by id against the supplied
/// <see cref="DbContext"/> instead of being walked into; navigation collections are
/// loaded on demand; and items removed from identifiable collections are deleted
/// through EF Core's change tracker.
/// </summary>
public class EfCoreDynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor, DbContext dbContext)
    : DynamicEntityMapper(serviceProvider, rqlMapAccessor), IEfCoreDynamicEntityMapper
{
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    protected override bool UseAssignForPlatformEntities => true;

    /// <inheritdoc />
    protected override async Task<object?> FindEntityAsync(Type entityType, object entity)
    {
        var id = (entity as IPlatformObject)?.Id;

        if (id == null)
            return null;

        return await _dbContext.FindAsync(entityType, id);
    }

    /// <inheritdoc />
    protected override async Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty)
    {
        var entry = GetEntry(entity);

        if (entry == null)
            return false;

        var collectionName = collectionProperty.Name;

        var nav = entry.Metadata.FindNavigation(collectionName);

        if (nav == null && entry.Metadata.FindSkipNavigation(collectionName) == null)
            return false;

        // Owned collections are loaded eagerly; calling LoadAsync on them throws.
        if (nav?.TargetEntityType.IsOwned() == true)
            return true;

        var collection = entry.Collection(collectionName);

        if (collection.IsLoaded)
            return true;

        await collection.LoadAsync();
        return true;
    }

    private EntityEntry? GetEntry(object entity)
    {
        var entityType = _dbContext.Model.FindEntityType(entity.GetType());

        if (entityType == null)
            return null;

        return _dbContext.Entry(entity);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage(Justification = "Reverse foreign-key handling requires an EF Core navigation whose FK lives on the dependent side; the InMemory provider used by the test fixtures cannot model that shape so the reverse-FK branch is unreachable under tests.")]
    protected override async Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity)
    {
        var entry = GetEntry(declaringObject);
        if (entry == null)
            return 0;

        var entityType = _dbContext.Model.FindEntityType(declaringObject.GetType())!;
        var navigation = entityType.FindNavigation(property);

        if (navigation == null)
            return 0;

        var fk = navigation.ForeignKey.Properties[0];

        string? id;
        if (entity is IPlatformObject platformEntity)
        {
            id = platformEntity.Id;
        }
        else if (entity == null)
        {
            id = null;
        }
        else
        {
            return 0;
        }

        if (!fk.DeclaringType.ClrType.IsAssignableFrom(entityType.ClrType))
        {
            // The FK lives on the dependent side (reverse reference): the navigation points
            // to a child whose table carries the FK back to us. We need to find that child
            // by id and stamp our own id into its FK column.
            if (entity == null)
                return 0;

            id = entry.CurrentValues.GetValue<string?>(nameof(IPlatformObject.Id));

            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException(
                    $"Unable to find id of the dependent entity '{entityType.ClrType.Name}'. It's possible that Id was not yet assigned. " +
                    $"Attempted to assign a reference '{fk.DeclaringType.ClrType.Name}' to '{entityType.ClrType.Name}'. " +
                    $"Either ensure id is assigned or try to use reverse assignment instead: '{entityType.ClrType.Name}' to '{fk.DeclaringType.ClrType.Name}'.");

            var refEntity = await FindEntityAsync(fk.DeclaringType.ClrType, entity);
            if (refEntity != null)
            {
                return TrySetForeignKey(_dbContext.Entry(refEntity), fk.Name, id);
            }

            return 0;
        }
        else
        {
            return TrySetForeignKey(entry, fk.Name, id);
        }
    }

    private static int TrySetForeignKey(EntityEntry entry, string foreignKeyName, string? id)
    {
        var existingId = entry.CurrentValues.GetValue<string?>(foreignKeyName);

        // Only set the reference if it has changed. In the null case, EF Core may decide the
        // referenced entity was deleted and cascade-delete the current entity — so avoid
        // pointlessly stamping the same value back in.
        if (existingId != id)
        {
            entry.CurrentValues[foreignKeyName] = id;
            return 1;
        }

        return 0;
    }

    /// <inheritdoc />
    protected override Task EnsureEntityRemovedAsync(object entity)
    {
        _dbContext.Remove(entity);
        return Task.CompletedTask;
    }
}
