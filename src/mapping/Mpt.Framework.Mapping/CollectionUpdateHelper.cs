using System.Collections;

namespace Mpt.Framework.Mapping;

internal static class CollectionUpdateHelper
{
    public static int ProcessPrimitiveCollection(IList target, IEnumerable<object> source)
    {
        var isChanged = false;
        var index = 0;
        var hasItems = false;

        foreach (var item in source)
        {
            hasItems = true;

            if (index >= target.Count)
            {
                isChanged = true;
                break;
            }

            if (!Equals(target[index], item))
            {
                isChanged = true;
                break;
            }

            index++;
        }

        if (!hasItems && target.Count == 0)
            return 0;

        if (hasItems && !isChanged && index == target.Count)
            return 0;

        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }

        return 1;
    }

    public static async Task<(List<UpdatedItem> Items, int UpdateCount)> ProcessUpdatableCollection(
        Type itemType,
        IEnumerable<object> source,
        IList target,
        Func<object, Type, Task<object>>? dbObjectFactory = null,
        Func<object, Task>? onEntityRemoved = null)
    {
        var targetItemType = target.GetType().GetGenericArguments().Single();

        var isPlatformObject = TypeHelper.IsPlatformObject(itemType);

        if (!isPlatformObject)
            target.Clear();

        List<UpdatedItem> result = [];

        var updateCount = 0;

        foreach (var item in source)
        {
            var isAdded = false;

            var dbItem = FindObjectInCollection(target, item);

            if (dbItem == null)
            {
                if (dbObjectFactory != null)
                {
                    dbItem = await dbObjectFactory.Invoke(item, targetItemType);
                }
                else
                {
                    dbItem = Activator.CreateInstance(targetItemType)!;
                }

                target.Add(dbItem);
                isAdded = true;
                updateCount++;
            }

            result.Add(new UpdatedItem(item, dbItem, isAdded));
        }

        if (isPlatformObject)
            updateCount += await RemoveUnprocessedItems(target, result.Select(s => s.Target).ToHashSet()!, onEntityRemoved);

        return (result, updateCount);
    }

    public static async Task<int> ProcessAssignableCollection(
        Type itemType,
        IEnumerable<IPlatformEntity> source,
        IList target,
        Func<Type, IPlatformEntity, Task<object?>> targetEntityLookup)
    {
        _ = itemType;

        // Platform entities can only be added to or removed from the collection;
        // they cannot be updated in place.
        var dbItemType = target.GetType().GetGenericArguments().Single();

        var updateCount = 0;

        HashSet<object> processed = [];
        foreach (var item in source)
        {
            var dbItem = FindObjectInCollection(target, item);

            if (dbItem == null)
            {
                dbItem = await targetEntityLookup(dbItemType, item);

                if (dbItem == null)
                    throw new KeyNotFoundException($"No persistence entity found for platform entity with id '{item.Id}'.");

                target.Add(dbItem);
                updateCount++;
            }

            processed.Add(dbItem);
        }

        updateCount += await RemoveUnprocessedItems(target, processed);

        return updateCount;
    }

    private static async Task<int> RemoveUnprocessedItems(IList dbCollection, HashSet<object> processed, Func<object, Task>? onEntityRemoved = null)
    {
        var removedCount = 0;
        for (var i = dbCollection.Count - 1; i >= 0; i--)
        {
            var item = dbCollection[i]!;
            if (!processed.Contains(item))
            {
                dbCollection.RemoveAt(i);

                if (onEntityRemoved != null)
                    await onEntityRemoved(item);

                removedCount++;
            }
        }

        return removedCount;
    }

    private static object? FindObjectInCollection(IEnumerable? collection, object item)
    {
        if (collection == null)
            return null;

        var id = TypeHelper.GetPlatformEntityId(item);

        if (id == null)
            return null;

        return collection.OfType<IPlatformObject>().FirstOrDefault(x => x.Id == id);
    }

    public record UpdatedItem(object Source, object Target, bool IsAdded);
}
