using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.MessageHub.Internal;

/// <summary>
/// For the in-memory transport (which has no SQL rule filters) — a single receive endpoint
/// pulls every <see cref="EventMessage"/> and this consumer fans out to the correct
/// per-stream user consumer, applying the same filter logic that the Service Bus side
/// gets for free via subscription rules.
/// </summary>
internal class InMemoryMessageConsumer(IBusRegistrationContext busContext, string moduleName, InputStream stream)
    : IConsumer<EventMessage>
{
    public async Task Consume(ConsumeContext<EventMessage> context)
    {
        if (!StreamRoutingHelper.ConditionSatisfied(moduleName, context.Message, stream))
            return;

        using var scope = busContext.CreateScope();
        var consumer = (IConsumer<EventMessage>)scope.ServiceProvider.GetRequiredService(stream.ConsumerType);
        await consumer.Consume(context);
    }
}
