using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Non-generic marker for event-producer implementations — exists so DI can locate
/// them by interface during assembly scanning.
/// </summary>
public interface IEntityEventProducer;

/// <summary>
/// Per-entity event-producer hook. Invoked by <see cref="IRepository{TEntity}"/> in
/// the after-save phase to turn the just-committed entity state into a stream of
/// <see cref="EventMessage"/> instances. The unit of work then hands each message to
/// the registered <see cref="IMessageHubPublisher"/>.
/// </summary>
/// <remarks>
/// The default implementation in the engine package returns no events. Override
/// <see cref="ProduceAsync"/> to emit lifecycle events; override <see cref="ShouldProduceOn"/>
/// to short-circuit when a particular action shouldn't produce anything.
/// </remarks>
public interface IEntityEventProducer<TEntity> : IEntityEventProducer where TEntity : class, IPlatformEntity
{
    /// <summary>
    /// Returns <see langword="true"/> if <see cref="ProduceAsync"/> should be invoked
    /// for the given action. Default-impl returns <see langword="true"/>.
    /// </summary>
    bool ShouldProduceOn(EntityAction action) => true;

    /// <summary>
    /// Produces the event-message stream for a single entity action. <paramref name="original"/>
    /// is supplied for updates and is <see langword="null"/> for creates / deletes.
    /// </summary>
    IAsyncEnumerable<EventMessage> ProduceAsync(EntityAction action, TEntity current, TEntity? original, CancellationToken cancellationToken);
}
