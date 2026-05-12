namespace Mpt.Framework.MessageHub;

/// <summary>
/// Factory helpers that build <see cref="EventMessageObject"/> instances from
/// <see cref="IPlatformEntity"/> data.
/// </summary>
public static class EventObjectFactory
{
    public static EventMessageObject MakeAdditional<TEntity>(
        TEntity entity,
        string key)
        where TEntity : IPlatformEntity
        => Make(entity, key, EventMessageObjectCategory.AdditionalEntity);

    public static EventMessageObject MakeAdditionalCustom(
        object data,
        string id,
        string key,
        string entityName)
        => new()
        {
            Id = id,
            Key = key,
            Type = entityName,
            Category = EventMessageObjectCategory.AdditionalEntity,
            Data = data
        };

    public static EventMessageObject Make<TEntity>(
        TEntity entity,
        string key,
        bool isOriginalValue = false)
        where TEntity : IPlatformEntity
        => Make(entity, key, isOriginalValue ? EventMessageObjectCategory.OriginalEntity : EventMessageObjectCategory.CurrentEntity);

    public static EventMessageObject Make<TEntity>(
        TEntity entity,
        string key,
        EventMessageObjectCategory category)
        where TEntity : IPlatformEntity
        => new()
        {
            Id = entity.Id,
            Key = key,
            Type = typeof(TEntity).Name,
            Category = category,
            Data = entity
        };
}
