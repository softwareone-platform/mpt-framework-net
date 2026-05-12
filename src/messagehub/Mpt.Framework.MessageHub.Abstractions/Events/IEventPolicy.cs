namespace Mpt.Framework.MessageHub;

/// <summary>
/// Fluent declaration of which <see cref="EntityAction"/> values an
/// <see cref="IEntityEventProducer{TEntity}"/> reacts to. Subclasses of
/// <c>EntityEventProducer&lt;TEntity&gt;</c> override <c>ConfigureEvents</c> and call
/// <see cref="Define"/> for each action they want to participate in.
/// </summary>
public interface IEventPolicy<TEntity>
{
    /// <summary>Marks the producer as interested in <paramref name="action"/>.</summary>
    IEventPolicy<TEntity> Define(EntityAction action);
}

internal class EventPolicy<TEntity> : IEventPolicy<TEntity>
{
    private readonly HashSet<EntityAction> _definedEvents = [];

    public bool IsDefined(EntityAction action) => _definedEvents.Contains(action);

    public IEventPolicy<TEntity> Define(EntityAction action)
    {
        if (!_definedEvents.Add(action))
            throw new InvalidOperationException($"Event {action} is already registered");

        return this;
    }
}
