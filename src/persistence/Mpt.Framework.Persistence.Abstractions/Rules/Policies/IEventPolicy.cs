namespace Mpt.Framework.Persistence;

/// <summary>
/// Configures which entity actions an event producer should fire on. A producer's
/// <c>ShouldProduceOn(action)</c> consults this policy.
/// </summary>
public interface IEventPolicy<TEntity>
{
    /// <summary>Declares that events should be produced when the given action runs.</summary>
    IEventPolicy<TEntity> Define(EntityAction action);
}
