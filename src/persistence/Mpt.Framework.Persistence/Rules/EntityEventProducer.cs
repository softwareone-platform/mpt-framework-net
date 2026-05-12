using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Internal;
using System.Runtime.CompilerServices;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Default no-op event producer. Override <see cref="ConfigureEvents"/> to declare
/// which actions trigger event production, and <see cref="ProduceAsync"/> to emit
/// <see cref="EventMessage"/> instances that the unit of work hands to
/// <see cref="IMessageHubPublisher"/>.
/// </summary>
public class EntityEventProducer<TEntity> : IEntityEventProducer<TEntity>
    where TEntity : class, IPlatformEntity
{
    private readonly Lazy<EventPolicy<TEntity>> _eventPolicy;

    /// <summary>Initialises lazily-built event-policy state.</summary>
    public EntityEventProducer()
    {
        _eventPolicy = new Lazy<EventPolicy<TEntity>>(() =>
        {
            var policy = new EventPolicy<TEntity>();
            ConfigureEvents(policy);
            return policy;
        });
    }

    /// <inheritdoc />
    public virtual bool ShouldProduceOn(EntityAction action) => _eventPolicy.Value.IsDefined(action);

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<EventMessage> ProduceAsync(
        EntityAction action,
        TEntity current,
        TEntity? original,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Override to declare which actions this producer fires on. Default is none —
    /// override and call <c>policy.Define(EntityAction.Create)</c> etc. The producer is
    /// only invoked by the repository when <see cref="ShouldProduceOn"/> returns true.
    /// </summary>
    protected virtual void ConfigureEvents(IEventPolicy<TEntity> policy) { }
}
