# Mpt.Framework.MessageHub.Abstractions

Pure abstractions for [Mpt.Framework.MessageHub](https://www.nuget.org/packages/Mpt.Framework.MessageHub). Contains only POCOs and interfaces — **no third-party runtime dependencies**, no MassTransit, no Azure Service Bus.

## When to install

Reference this package from your **Application layer** (or any project that needs to *declare what it consumes* or *publish events*, but should not depend on the transport engine):

```
dotnet add package Mpt.Framework.MessageHub.Abstractions
```

Install the full engine package `Mpt.Framework.MessageHub` only in your **composition root** (Worker / API host).

This keeps your domain and application code free of MassTransit / Service Bus references.

## What's inside

| Type                    | Purpose                                                              |
| ----------------------- | -------------------------------------------------------------------- |
| `EventMessage`          | Wire payload — id, routing, objects, info, timestamp, hints, session/partition keys. |
| `EventMessageRouting`   | Stream type, source/target modules, entity, event, optional delay.   |
| `EventMessageObject`    | Subject of the event (current entity, original, custom, etc.).       |
| `EventMessageInfo`      | Human-readable summary / description.                                |
| `StreamTypes`           | `Events` / `Sync` / `System` flags enum.                             |
| `EventHints`            | `Incomplete` / `Silent` / `SoftSync` downstream hints.               |
| `IMessageHubPublisher`  | The publish API — used wherever events are emitted.                  |
| `InputStreamProvider`   | Abstract base — derive once per consumer module to declare streams.  |
| `InputStream<TConsumer>`| Strongly-typed declaration of "this consumer should receive these events". |
| `InputStreamFilter`     | Per-stream filter on source modules, entities, and event names.      |
| `InputStreamSettings`   | Per-stream transport tuning (prefetch, lock duration, sessions, retry). |
| `StreamNameValidator`   | Validates stream / module / provider names.                          |

## Defining an input stream (Application layer)

```csharp
using MassTransit;
using Mpt.Framework.MessageHub;

public class AccountEventsProvider : InputStreamProvider
{
    public override string Key => "main";

    public override IEnumerable<InputStream> GetInputStreams()
    {
        yield return DefineStream<AccountEventConsumer>("accounts", StreamTypes.Events, input =>
        {
            input.Filter.Modules = ["accounts"];
            input.Filter.Entities = ["Account", "Buyer", "Seller"];
        });
    }
}

public class AccountEventConsumer : IConsumer<EventMessage>
{
    public Task Consume(ConsumeContext<EventMessage> context)
    {
        // process context.Message
        return Task.CompletedTask;
    }
}
```

> Note: the consumer implements `MassTransit.IConsumer<EventMessage>`. That means consumers themselves take a MassTransit dep — only the stream **definition** is in this abstractions package.

## Publishing an event

```csharp
public sealed class AccountService(IMessageHubPublisher publisher)
{
    public async Task NotifyAccountCreated(Account account, CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(new EventMessage
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Info = new EventMessageInfo { Summary = $"Account {account.Name} was created" },
            Routing = new EventMessageRouting
            {
                Stream = StreamTypes.Events,
                SourceModule = "accounts",
                Entity = "Account",
                Event = "Created",
            },
            Objects =
            [
                new EventMessageObject
                {
                    Id = account.Id,
                    Key = "account",
                    Category = EventMessageObjectCategory.CurrentEntity,
                    Data = account,
                }
            ],
        }, cancellationToken);
    }
}
```

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
