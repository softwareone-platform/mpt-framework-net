using Mpt.Rql;
using Mpt.Rql.Abstractions;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Framework.Mapping;

internal class MappingExecutor
{
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly DynamicEntityMapper _mapper;

    private readonly Queue<PropertyInfo>? _pathFilter;

    public MappingExecutor(IRqlMapAccessor rqlMapAccessor, DynamicEntityMapper mapper, LambdaExpression? pathExpression = null)
    {
        _rqlMapAccessor = rqlMapAccessor;
        _mapper = mapper;

        if (pathExpression != null)
        {
            var pathStack = GetPropertyStack(pathExpression);
            _pathFilter = new Queue<PropertyInfo>(pathStack!.Reverse());
        }
    }

    public async Task<int> MapInternalAsync(Type typeFrom, object? from, object to, DynamicEntityMapper.MapModes mode, RqlMapEntry? parentMap)
    {
        var entries = parentMap?.InlineMap != null
            ? parentMap.InlineMap.Values
            : _rqlMapAccessor.GetEntries(to.GetType(), typeFrom); // map is reversed

        var deferred = new Queue<RqlMapEntry>(10);

        var updateCount = await ProcessMapEntriesAsync(from, to, entries, deferred, mode);

        while (deferred.Count > 0)
        {
            var entry = deferred.Dequeue();
            updateCount += await ProcessMapEntryAsync(entry, from, to);
        }

        return updateCount;
    }

    private async Task<int> ProcessMapEntriesAsync(object? from, object to, IEnumerable<RqlMapEntry> entries, Queue<RqlMapEntry> deferred, DynamicEntityMapper.MapModes mode)
    {
        var updateCount = 0;

        foreach (var entry in entries)
        {
            if (!ShouldProcessEntry(entry, mode))
                continue;

            var shouldExitLoop = ApplyPathFiltering(entry);

            updateCount += await ProcessEntryBasedOnType(entry, deferred, from, to);

            if (shouldExitLoop)
                break;
        }

        return updateCount;
    }

    private static bool ShouldProcessEntry(RqlMapEntry entry, DynamicEntityMapper.MapModes mode)
    {
        if (!mode.HasFlag(DynamicEntityMapper.MapModes.Complex) && entry.TargetProperty.Type != RqlPropertyType.Primitive)
            return false;

        if (!mode.HasFlag(DynamicEntityMapper.MapModes.Primitive) && entry.TargetProperty.Type == RqlPropertyType.Primitive)
            return false;

        return true;
    }

    private bool ApplyPathFiltering(RqlMapEntry entry)
    {
        if (_pathFilter == null || !_pathFilter.TryPeek(out var filterProperty))
            return false;

        var sourceProperty = entry.TargetProperty.Property;
        if (sourceProperty.Name != filterProperty.Name)
            return false;

        // This is the property we want to process - advance the filter and exit after processing.
        _pathFilter.Dequeue();
        return true;
    }

    private async Task<int> ProcessEntryBasedOnType(RqlMapEntry entry, Queue<RqlMapEntry> deferred, object? from, object to)
    {
        if (entry.TargetProperty.Type != RqlPropertyType.Primitive)
        {
            deferred.Enqueue(entry);
            return 0;
        }

        return await ProcessMapEntryAsync(entry, from, to);
    }

    private async Task<int> ProcessMapEntryAsync(RqlMapEntry entry, object? updated, object dbEntity)
    {
        var updateCount = 0;
        // mapping is inversed: target property is now source.
        var sourceRqlProperty = entry.TargetProperty;

        // Ignore properties that are not mapped.
        if (sourceRqlProperty.Mode == RqlPropertyMode.Ignored)
            return updateCount;

        if (entry.FactoryType is not null)
        {
            if (typeof(IUpdatableMappingFactory).IsAssignableFrom(entry.FactoryType))
                return ProcessFactoryBasedMapping(entry, updated, dbEntity);

            // If a factory is specified but does not implement IUpdatableMappingFactory, the
            // mapping cannot be performed; skip the update.
            return updateCount;
        }

        var (canMap, dbPropertyObject, dbProperty) = GetDbProperty(dbEntity!, entry);

        if (dbProperty != null && !dbProperty.CanWrite)
            return updateCount;

        if (!canMap)
            return updateCount;

        var updatedValue = sourceRqlProperty.Property.GetValue(updated);

        // Reference props sometimes may be mapped to a parameter expression, so the property
        // won't be there. In such cases validation is unnecessary; in all other cases the
        // exception must be thrown when the property is null.
        if (dbProperty == null && sourceRqlProperty.Type != RqlPropertyType.Reference)
            throw MakePropertyCannotBeMappedException(sourceRqlProperty);

        updateCount += sourceRqlProperty.Type switch
        {
            RqlPropertyType.Primitive => ProcessPrimitive(dbProperty, dbPropertyObject, updatedValue),
            RqlPropertyType.Reference => await ProcessReference(sourceRqlProperty, entry, dbProperty, dbPropertyObject, updatedValue),
            RqlPropertyType.Collection => await ProcessCollection(sourceRqlProperty, entry, dbProperty!, dbPropertyObject, updatedValue),
            _ => throw new InvalidOperationException($"Unknown property type [{entry.TargetProperty.Type}]"),
        };

        return updateCount;
    }

    private int ProcessFactoryBasedMapping(RqlMapEntry entry, object? updated, object dbEntity)
    {
        var factoryType = entry.FactoryType!;
        var factory = _mapper.ServiceProvider.GetService(factoryType) as IUpdatableMappingFactory
            ?? throw new InvalidOperationException(
                $"Factory of type '{factoryType.Name}' is not registered in the service provider. " +
                $"Register it in DI (e.g. services.AddScoped<{factoryType.Name}>()) before invoking the mapper. " +
                $"Factories used at update time must implement IUpdatableMappingFactory.");

        // Extract the property value from the source object.
        var sourceRqlProperty = entry.TargetProperty;
        var propertyValue = sourceRqlProperty.Property.GetValue(updated);

        factory.TryUpdate(propertyValue, dbEntity, out var hasChanges);
        return hasChanges ? 1 : 0;
    }

    private static int ProcessPrimitive(PropertyInfo? dbProperty, object dbPropertyOwner, object? updatedValue)
    {
        if (updatedValue == null)
        {
            return SetPropertyNull(dbProperty, dbPropertyOwner);
        }

        var targetValue = dbProperty!.GetValue(dbPropertyOwner);

        if (!Equals(targetValue, updatedValue))
        {
            return SetPropertyValue(dbProperty, dbPropertyOwner, updatedValue);
        }

        return 0;
    }

    private async Task<int> ProcessReference(IRqlPropertyInfo sourceProperty, RqlMapEntry entry, PropertyInfo? dbProperty, object dbPropertyOwner, object? updatedValue)
    {
        // Mapping platform-object references is a special case.
        if (TypeHelper.IsPlatformObject(sourceProperty.Property.PropertyType) && _mapper.UseAssignForPlatformEntities)
        {
            return await _mapper.UpdatePlatformEntityReference(dbPropertyOwner, dbProperty!, updatedValue);
        }

        if (updatedValue == null)
        {
            return SetPropertyNull(dbProperty, dbPropertyOwner);
        }

        var updateCount = 0;
        object? objectToUpdate = null;
        if (dbProperty != null)
        {
            objectToUpdate = dbProperty.GetValue(dbPropertyOwner);

            if (objectToUpdate == null)
            {
                objectToUpdate = CreateInstance(dbProperty)!;
                updateCount += SetPropertyValue(dbProperty, dbPropertyOwner, objectToUpdate);
            }
        }
        else if (entry.SourceExpression?.Body is ParameterExpression)
        {
            objectToUpdate = dbPropertyOwner;
        }

        if (objectToUpdate == null)
            throw MakePropertyCannotBeMappedException(sourceProperty);

        updateCount += await MapInternalAsync(sourceProperty.Property.PropertyType, updatedValue, objectToUpdate!, DynamicEntityMapper.MapModes.All, entry);
        return updateCount;
    }

    private async Task<int> ProcessCollection(IRqlPropertyInfo sourceProperty, RqlMapEntry entry, PropertyInfo dbProperty, object dbPropertyOwner, object? updatedValue)
    {
        if (updatedValue == null)
        {
            return SetPropertyNull(dbProperty, dbPropertyOwner);
        }

        if (!typeof(IList).IsAssignableFrom(dbProperty.PropertyType))
            throw new InvalidOperationException($"Property {dbProperty.Name} of type {dbProperty.DeclaringType!.Name} has to implement IList in order to be automatically mapped");

        var itemType = sourceProperty.ElementType!;

        if (updatedValue is not IEnumerable<object> updatedCollection)
            return 0;

        // If the collection cannot be loaded it isn't mapped; skip the update.
        if (!await _mapper.EnsureCollectionLoadedAsync(dbPropertyOwner, dbProperty))
            return 0;

        var dbCollection = GetDbCollection(dbProperty, dbPropertyOwner);

        if (!TypeHelper.IsUserComplexType(itemType))
        {
            return CollectionUpdateHelper.ProcessPrimitiveCollection(dbCollection!, updatedCollection);
        }
        else if (TypeHelper.IsPlatformEntity(itemType) && _mapper.UseAssignForPlatformEntities)
        {
            return await CollectionUpdateHelper.ProcessAssignableCollection(itemType, updatedCollection.OfType<IPlatformEntity>(), dbCollection, _mapper.FindEntityAsync);
        }
        else
        {
            var (updated, updateCount) = await CollectionUpdateHelper.ProcessUpdatableCollection(itemType, updatedCollection, dbCollection, async (src, dbType) =>
            {
                var dbItem = Activator.CreateInstance(dbType)!;
                var primitiveExecutor = new MappingExecutor(_rqlMapAccessor, _mapper);
                await primitiveExecutor.MapInternalAsync(itemType, src, dbItem!, DynamicEntityMapper.MapModes.Primitive, entry);
                return dbItem;
            }, _mapper.EnsureEntityRemovedAsync);

            foreach (var item in updated)
            {
                var itemExecutor = new MappingExecutor(_rqlMapAccessor, _mapper);
                updateCount += await itemExecutor.MapInternalAsync(itemType, item.Source, item.Target!, DynamicEntityMapper.MapModes.All, entry);
            }

            return updateCount;
        }
    }

    private static IList GetDbCollection(PropertyInfo dbProperty, object dbPropertyOwner)
    {
        if (dbProperty.GetValue(dbPropertyOwner) is not IList existingData)
        {
            existingData = (IList)CreateInstance(dbProperty)!;
            dbProperty.SetValue(dbPropertyOwner, existingData);
        }

        return existingData;
    }

    private static (bool, object, PropertyInfo?) GetDbProperty(object root, RqlMapEntry entry)
    {
        var currentObject = root;

        if (entry.SourceExpression == null)
        {
            return (false, null!, null);
        }

        var propertyStack = GetPropertyStack(entry.SourceExpression);

        if (propertyStack == null)
        {
            // Complex and conditional mappings are not supported.
            return (false, null!, null);
        }

        var targetPropertyInfo = entry.TargetProperty.Property;

        if (TypeHelper.IsPlatformEntity(targetPropertyInfo.PropertyType) && propertyStack.Count > 1)
        {
            // Nested platform-entity properties are not supported.
            return (false, null!, null);
        }

        PropertyInfo? property;

        while (propertyStack.TryPop(out property))
        {
            if (propertyStack.Count == 0)
                break;

            var iterationObject = property.GetValue(currentObject);

            if (iterationObject == null && TypeHelper.IsUserComplexType(property.PropertyType))
            {
                iterationObject = CreateInstance(property);
                property.SetValue(currentObject, iterationObject);
            }

            currentObject = iterationObject!;
        }

        return (true, currentObject!, property);
    }

    private static object? CreateInstance(PropertyInfo property)
    {
        try
        {
            return Activator.CreateInstance(property.PropertyType);
        }
        catch
        {
            throw new InvalidOperationException($"Could not create instance of type [{property.PropertyType}]. Please make sure it has a default constructor.");
        }
    }

    private static Stack<PropertyInfo>? GetPropertyStack(LambdaExpression expression)
    {
        var chain = new Stack<PropertyInfo>();

        var currentExpression = expression.Body;

        // Handle unboxing conversion (when a property returns object but the lambda expects object).
        if (currentExpression is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpr)
        {
            currentExpression = unaryExpr.Operand;
        }

        while (currentExpression is not ParameterExpression)
        {
            if (currentExpression is not MemberExpression memberExpression || memberExpression.Member is not PropertyInfo property)
            {
                return null;
            }

            chain.Push(property);
            currentExpression = memberExpression.Expression!;
        }

        return chain;
    }

    private static int SetPropertyValue(PropertyInfo property, object owner, object? value)
    {
        property.SetValue(owner, value);
        return 1;
    }

    private static int SetPropertyNull(PropertyInfo? property, object owner)
    {
        if (property == null)
            return 0;

        var targetValue = property.GetValue(owner);
        if (targetValue == null)
            return 0;

        property.SetValue(owner, null);
        return 1;
    }

    private static InvalidOperationException MakePropertyCannotBeMappedException(IRqlPropertyInfo sourceProperty)
        => new($"Property {sourceProperty.Property.Name} of type {sourceProperty.Property.DeclaringType!.Name} is not mapped, or mapping is invalid.");
}
