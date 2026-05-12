namespace Mpt.Framework.Persistence.Internal;

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
