# Mpt.Framework.MessageHub

A MassTransit-based event bus for module-to-module pub/sub. Publishers emit `EventMessage` payloads; consumers subscribe via `InputStreamProvider` declarations that filter events server-side via Azure Service Bus SQL rules (or client-side on the in-memory transport).

## Install

Install in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.MessageHub
```

In your Application layer, depend on the lightweight [`Mpt.Framework.MessageHub.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.MessageHub.Abstractions) instead — that gives you `EventMessage`, `IMessageHubPublisher`, `InputStreamProvider`, etc. without pulling MassTransit into your domain projects.

Target framework: **net10.0**.

## Registration

```csharp
using Mpt.Framework.MessageHub;

services.AddMessageHub("billing", hub =>
{
    hub.Settings.Transport = MessageHubTransport.ServiceBus;  // or .InMemory for tests
    hub.Settings.ConnectionString = configuration.ServiceBus.ConnectionString;

    // declare what events this module consumes
    hub.ConfigureInput<AccountEventsProvider>();
});
```

The first argument is your **module code** — used to build subscription names and to exclude the module's own events from its own consumers by default.

### Settings

| Setting              | Default                                | Purpose                                                |
| -------------------- | -------------------------------------- | ------------------------------------------------------ |
| `Transport`          | `ServiceBus`                           | `InMemory` (tests) or `ServiceBus` (Azure).            |
| `ConnectionString`   | —                                      | Required for `ServiceBus` transport.                   |
| `OutputStream`       | `marketplace.platform.messages`        | Topic / queue name for outbound traffic.               |
| `CleanupMode`        | `None`                                 | `DeleteEmptyUnknown` / `DeleteAnyUnknown` — sweeps stale subscriptions on startup. |
| `OnMessagePublishing`| `null`                                 | Optional hook invoked just before every publish.        |
| `PublishMode`        | `Immediate`                            | `Immediate` awaits each send inline; `Background` queues to an in-process channel drained by a hosted service. |

## Publishing

Inject `IMessageHubPublisher` from the abstractions package:

```csharp
public sealed class AccountService(IMessageHubPublisher publisher)
{
    public Task PublishAccountCreated(Account account, CancellationToken cancellationToken) =>
        publisher.PublishAsync(MakeMessage(account), cancellationToken);
}
```

Each call sets the routing headers (`mpt_stream_type`, `mpt_source_module`, `mpt_target_modules`, `mpt_entity`, `mpt_event`) that consuming subscriptions filter on. See [`MessageHubHeaders`](./MessageHubHeaders.cs) for the canonical names.

## Consuming

Declare an `InputStreamProvider` (see the [abstractions package README](https://www.nuget.org/packages/Mpt.Framework.MessageHub.Abstractions)). Each `InputStream<TConsumer>` becomes:

- **On Service Bus** — a subscription on the configured topic with a SQL rule filter derived from `InputStreamFilter`. The filter is applied server-side.
- **On in-memory** — a single receive endpoint with a built-in fan-out consumer that re-applies the same filter logic in-process.

The user-facing consumer must implement `MassTransit.IConsumer<EventMessage>`.

## How filtering works

A stream's `InputStreamFilter` declares which `Modules` / `Entities` / `Events` it accepts. The default rule on every subscription also:

- Includes events whose `TargetModules` is empty (broadcast) **or** contains this module's name.
- Limits to streams whose `Sources` flag matches the message's `Routing.Stream`.
- Excludes events the module itself emitted, unless `AllowOwnEvents = true`.

## Event authoring (`Generic*Event<TEntity>` + `IPlatformEventEmitter`)

`AddMessageHub(...)` also wires an event-authoring layer for callers who want to compose lifecycle events without hand-building `EventMessage` instances. Inject `IPlatformEventEmitter` from your application code, register events during the unit of work, and flush them once at the end.

```csharp
public sealed class AccountService(IPlatformEventEmitter emitter, MessageHubBuilder hub)
{
    public async Task UpdateAsync(Account current, Account original, CancellationToken ct)
    {
        // ... persist the entity ...

        emitter.Register(new GenericUpdatedEvent<Account>(
            module: hub.ModuleCode,
            data: current,
            original: original,
            permissionsBuilder: new PlatformEventPermissionsBuilder()
                .AddAccountPrincipalAccess(current.Id, accountType: "Tenant")));

        await emitter.EmitAsync(ct);
    }
}
```

The built-in event classes live in `Mpt.Framework.MessageHub.Abstractions` so your application layer doesn't have to reference the engine package:

| Class                                      | `EventKey`        | Notes                                                 |
| ------------------------------------------ | ----------------- | ----------------------------------------------------- |
| `GenericCreatedEvent<TEntity>`             | `created`         | Carries the new entity as the main object.            |
| `GenericUpdatedEvent<TEntity>`             | `updated`         | Optional `original` baseline appended as `OriginalEntity`. |
| `GenericDeletedEvent<TEntity>`             | `deleted`         | Carries only `Id`; marks `EventHints.Incomplete` automatically. |
| `GenericStatusChangedEvent<TEntity>`       | `status_changed`  | Same shape as Updated plus a `statusResolver` delegate; suppresses any subsequent Updated event for the same entity in the current scope. |
| `CustomEvent<TEntity>`                     | descriptor-driven | Used by `IEntityEventProducer<T>.ProduceCustomEvents`; configure key / summary / description via `Customize(...)`. |

### `EntityEventProducer<TEntity>` — declarative producer

For consumers who want the upstream-style per-entity producer shape, derive from `EntityEventProducer<TEntity>` and declare which actions you participate in:

```csharp
public sealed class AccountEventProducer(IServiceProvider sp) : EntityEventProducer<Account>(sp)
{
    protected override void ConfigureEvents(IEventPolicy<Account> context)
    {
        context.Define(EntityAction.Create);
        context.Define(EntityAction.Update);
        context.Define(EntityAction.Delete);
    }

    protected override Task ConfigurePermissionsAsync(
        PlatformEventPermissionsBuilder builder, Account entity, Account? original, CancellationToken ct)
    {
        builder.AddAccountPrincipalAccess(entity.Id, accountType: "Tenant");
        return Task.CompletedTask;
    }
}

// composition root
services.AddScoped<IEntityEventProducer<Account>, AccountEventProducer>();
```

The producer surface (`ProduceCreatedEvents`, `ProduceUpdatedEvents`, `ProduceStatusChangedEvents`, `ProduceDeletedEvents`, `RegisterCustomEvent` + `ProduceCustomEvents`, `CustomizeEvents`, `Reset`) builds the appropriate `Generic*Event<TEntity>` (or `CustomEvent<TEntity>` for ad-hoc events) and forwards it to the shared `IPlatformEventEmitter`.

### Naming clash with `Mpt.Framework.Persistence.IEntityEventProducer<TEntity>`

`Mpt.Framework.Persistence` defines its own `IEntityEventProducer<TEntity>` with a single `ProduceAsync` method that yields `EventMessage` instances directly — that interface is what the Persistence `Repository<T>` calls in its after-save phase. The MessageHub-side producer documented above lives at a different abstraction layer (it constructs `Generic*Event` instances and registers them with `IPlatformEventEmitter`). If you reference both namespaces in the same file, fully-qualify the type name; otherwise C# will report CS0104.

### Bridging to `Mpt.Framework.Persistence`

To emit `GenericCreatedEvent<TEntity>` etc. from inside a Persistence `Repository<T>` save flow, subclass the Persistence producer and call `MakeMessage()`:

```csharp
public sealed class AccountPersistenceEventProducer(MessageHubBuilder hub)
    : Mpt.Framework.Persistence.EntityEventProducer<Account>
{
    public override async IAsyncEnumerable<EventMessage> ProduceAsync(
        Mpt.Framework.Persistence.EntityAction action, Account current, Account? original,
        [EnumeratorCancellation] CancellationToken ct)
    {
        PlatformEvent @event = action switch
        {
            Mpt.Framework.Persistence.EntityAction.Create => new GenericCreatedEvent<Account>(hub.ModuleCode, current, new()),
            Mpt.Framework.Persistence.EntityAction.Update => new GenericUpdatedEvent<Account>(hub.ModuleCode, current, original, new()),
            Mpt.Framework.Persistence.EntityAction.Delete => new GenericDeletedEvent<Account>(hub.ModuleCode, current, new()),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        yield return @event.MakeMessage();
        await Task.CompletedTask;
    }
}
```

`moduleCode` should match the value you passed to `AddMessageHub(...)` — `MessageHubBuilder.ModuleCode` is the canonical source.

### Actor stamping

Register an `IPlatformEventActorProducer` to stamp the current principal onto every outgoing event:

```csharp
services.AddScoped<IPlatformEventActorProducer, MyActorProducer>();
```

Without a registration, the emitter does not add an `ActorInfo` object; events still publish.

### `IPlatformMessageReplayService`

Resolved as a scoped service. Call `ReplayAsync(message, module, ct)` to re-drive an `EventMessage` with the default `RetryPolicy` (3 attempts, linear delay). Customise the policy via the overloads. The service increments the message's `Replays` counter and aborts once it reaches `MaxAttempts`.

### `ISyncPlatformEventProducer<TEntity>`

Forward-compat marker interface. No concrete implementation ships in this package; intended for consumers who roll their own sync-stream pipeline.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
