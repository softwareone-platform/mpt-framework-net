using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mpt.Framework.MessageHub;

internal class PlatformEventEmitter : IPlatformEventEmitter
{
    private const string ACTOR_KEY = "actor";

    private readonly IPlatformMessagePublisher _publisher;
    private readonly IPlatformEventActorProducer? _actorProducer;
    private readonly ConcurrentQueue<TracedTransport<IPlatformEvent>> _events = [];

    public PlatformEventEmitter(IPlatformMessagePublisher publisher, IServiceProvider serviceProvider)
    {
        _publisher = publisher;
        _actorProducer = serviceProvider.GetService<IPlatformEventActorProducer>();
    }

    public void Register(IPlatformEvent entityEvent) => _events.Enqueue(new(entityEvent, Activity.Current?.Context));

    public void Register(IEnumerable<IPlatformEvent> entityEvents)
    {
        foreach (var entityEvent in entityEvents)
        {
            Register(entityEvent);
        }
    }

    public async Task EmitAsync(CancellationToken cancellationToken)
    {
        var emitTasks = new List<Task>();

        while (_events.TryDequeue(out var entityEvent))
        {
            emitTasks.Add(MakeAndPublishAsync(entityEvent.Message, entityEvent.ActivityContext, cancellationToken));
        }

        await Task.WhenAll(emitTasks);
    }

    public Task EmitSingleAsync(IPlatformEvent entityEvent, CancellationToken cancellationToken)
        => MakeAndPublishAsync(entityEvent, Activity.Current?.Context, cancellationToken);

    public void Reset() => _events.Clear();

    private async Task MakeAndPublishAsync(IPlatformEvent entityEvent, ActivityContext? activityContext, CancellationToken cancellationToken)
    {
        if (entityEvent is PlatformEvent pe && pe.IsSuppressed)
        {
            return;
        }

        var message = entityEvent.MakeMessage();

        if (_actorProducer is not null && !message.Objects.Exists(x => x.Category == EventMessageObjectCategory.ActorInfo))
        {
            var actor = await _actorProducer.GetActor(cancellationToken);
            if (actor is not null)
            {
                message.Objects.Add(new EventMessageObject
                {
                    Id = actor.Id,
                    Key = ACTOR_KEY,
                    Name = actor.Name,
                    Icon = actor.Icon,
                    Type = null,
                    Category = EventMessageObjectCategory.ActorInfo,
                    Data = actor
                });
            }
        }

        message.Validate();
        await _publisher.PublishAsync(new TracedTransport<EventMessage>(message, activityContext), cancellationToken);
    }
}
