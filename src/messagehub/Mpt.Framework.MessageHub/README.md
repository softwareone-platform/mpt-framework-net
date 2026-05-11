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

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
