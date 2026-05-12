namespace Mpt.Framework.MessageHub;

/// <summary>
/// Factory helpers that build <see cref="EventMessageObject"/> instances from
/// <see cref="IPlatformEntity"/> data.
/// </summary>
/// <remarks>
/// Upstream used a static <c>PlatformEntityMap</c> to resolve display names and read
/// <c>entity.Name</c>/<c>entity.Icon</c> from the (richer) upstream <c>IPlatformEntity</c>.
/// The OSS surface uses <c>typeof(TEntity).Name</c> for the type label and leaves
/// <see cref="EventMessageObject.Name"/> / <see cref="EventMessageObject.Icon"/> null —
/// consumers that need display values can override <c>GetMainObject</c> or attach extras
/// via <see cref="IEventDescriptor.AdditionalObjects"/>.
/// </remarks>
public static class EventObjectFactory
{
    /// <summary>Builds an <see cref="EventMessageObjectCategory.AdditionalEntity"/> object for <paramref name="entity"/>.</summary>
    public static EventMessageObject MakeAdditional<TEntity>(
        TEntity entity,
        string key)
        where TEntity : IPlatformEntity
        => Make(entity, key, EventMessageObjectCategory.AdditionalEntity);

    /// <summary>Builds an additional-entity object from arbitrary <paramref name="data"/>.</summary>
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

    /// <summary>Builds a CurrentEntity (or OriginalEntity when <paramref name="isOriginalValue"/> is true) object.</summary>
    public static EventMessageObject Make<TEntity>(
        TEntity entity,
        string key,
        bool isOriginalValue = false)
        where TEntity : IPlatformEntity
        => Make(entity, key, isOriginalValue ? EventMessageObjectCategory.OriginalEntity : EventMessageObjectCategory.CurrentEntity);

    /// <summary>Builds an entity object with an explicit category.</summary>
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
