using MassTransit;
using Mpt.Framework.Operations.Activities;
using Mpt.Framework.Operations.Models;
using Mpt.Framework.Operations.Models.Messages;
using System.Text.Json;

namespace Mpt.Framework.Operations;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

internal class OperationExecutor<TOperation, TTask, TSaga> : MassTransitStateMachine<TSaga>
    where TOperation : IOperationContract
    where TSaga : OperationSaga
{
    public OperationExecutor()
    {
        InstanceState(x => x.Status);

        Event(() => OperationStarting, t =>
        {
            t.InsertOnInitial = true;
            t.CorrelateById(context => context.Message.OperationMetadata.Id);
        });
        Event(() => OperationPreparing, x => x.CorrelateById(context => context.Message.OperationMetadata.Id));
        Event(() => BatchCompleted, x => x.CorrelateById(context => context.Message.OperationMetadata.Id));
        Event(() => OperationFailed, x => x.CorrelateById(context => context.Message.OperationMetadata.Id));
        Event(() => OperationCancelling, x => x.CorrelateById(context => context.Message.OperationMetadata.Id));
        Event(() => TaskReceived, x =>
        {
            x.ReadOnly = true;
            x.CorrelateById(context => context.Message.OperationMetadata.Id);
        });

        Initially(
            When(OperationStarting)
            .Then(context =>
            {
                context.Saga.Timestamps.Started = DateTimeOffset.UtcNow;
                context.Saga.Data = JsonSerializer.SerializeToNode(context.Message, OperationSerializerOptions.Default).AsObject();
            })
            .Activity(x => x.OfType<OperationStartingActivity<TOperation, TSaga>>())
            .TransitionTo(Preparing));

        During(Preparing,
            When(OperationPreparing)
            .Activity(x => x.OfType<OperationPreparingActivity<TOperation, TTask, TSaga>>())
            .TransitionTo(Running));

        During(Preparing, Running,
            When(TaskReceived).Activity(x => x.OfType<ProcessTaskActivity<TOperation, TTask, TSaga>>()),

            When(BatchCompleted)
                .Then(context =>
                {
                    var states = new OperationStateArray(context.Saga.TaskStates!, context.Saga.Statistics.Total);

                    foreach (var item in context.Message.Succeded)
                    {
                        states.Set(item, ItemState.Succeeded);
                    }

                    foreach (var item in context.Message.Failed)
                    {
                        states.Set(item, ItemState.Failed);
                    }

                    var counters = states.GetCounters();

                    context.Saga.Statistics.Succeded = counters.GetValueOrDefault(ItemState.Succeeded);
                    context.Saga.Statistics.Failed = counters.GetValueOrDefault(ItemState.Failed);
                    context.Saga.Statistics.Pending = counters.GetValueOrDefault(ItemState.Pending);
                })
                .If(context => context.Saga.Statistics.Succeded + context.Saga.Statistics.Failed == context.Saga.Statistics.Total,
                    b => b.TransitionTo(Completed)));

        // Handle failure and cancellation only in non-terminal states
        During(Initial, Preparing, Running,
            When(OperationFailed).Then(t =>
            {
                t.Saga.Failure = t.Message.Failure;
            }).TransitionTo(Failed),
            When(OperationCancelling).TransitionTo(Cancelled));

        // Ignore events in machine terminal states
        During(Failed, Cancelled, Completed,
            Ignore(OperationStarting),
            Ignore(OperationPreparing),
            Ignore(BatchCompleted),
            Ignore(OperationCancelling),
            Ignore(TaskReceived),
            Ignore(OperationFailed)
        );


        WhenEnter(Completed, t => t.Activity(x => x.OfType<OperationFinishedActivity<TOperation, TTask, TSaga>>()));
        WhenEnter(Cancelled, t => t.Activity(x => x.OfType<OperationFinishedActivity<TOperation, TTask, TSaga>>()));
        WhenEnter(Failed, t => t.Activity(x => x.OfType<OperationFinishedActivity<TOperation, TTask, TSaga>>()));

        SetCompletedWhenFinalized();
    }

    public State Preparing { get; set; }

    public State Running { get; set; }

    public State Completed { get; set; }

    public State Failed { get; set; }

    public State Cancelled { get; set; }

    public Event<OperationStartingMessage<TOperation>> OperationStarting { get; set; }

    public Event<OperationCancelledMessage> OperationCancelling { get; set; }

    public Event<OperationPreparingMessage<TOperation>> OperationPreparing { get; set; }

    public Event<TaskStartingMessage<TTask>> TaskReceived { get; set; }

    public Event<BatchCompletedMessage> BatchCompleted { get; set; }

    public Event<OperationFailedMessage> OperationFailed { get; set; }
}
