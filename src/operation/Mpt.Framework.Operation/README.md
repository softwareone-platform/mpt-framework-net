# Mpt.Framework.Operation

The MassTransit-based operation engine. Orchestrates long-running asynchronous work via a saga, dispatching task batches over an in-memory or Azure Service Bus transport.

Each operation is defined as a single class that:

1. **Starts** — optionally checks a start condition that can postpone the operation.
2. **Produces tasks** — emits a stream of task payloads from `IAsyncEnumerable`.
3. **Processes each task** — runs once per emitted payload, in parallel up to the configured concurrency.
4. **Finishes** — receives an aggregated result (total / succeeded / failed / cancelled).

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Operation
```

In your Application layer, depend on the lightweight [`Mpt.Framework.Operation.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Operation.Abstractions) instead — that gives you the `Operation<,>` base class and `IOperationDispatcher` without pulling MassTransit / EF Core into your domain projects.

For durable SQL Server persistence, add the companion package [`Mpt.Framework.Operation.EFCore`](https://www.nuget.org/packages/Mpt.Framework.Operation.EFCore). This package on its own ships with **in-memory persistence**, which is fine for tests and prototypes.

Target framework: **net10.0**.

## Registration

```csharp
using Mpt.Framework.Operation;
using Mpt.Framework.Operation.Configuration;

services.AddOperation("billing", ops =>
{
    ops.Settings.Mode = OperationMode.ConsumeAndDispatch;
    ops.Settings.Transport = OperationTransport.InMemory; // or OperationTransport.ServiceBus
    ops.Register<InvoiceBatchOperation>("invoice.batch", t => t.Tasks.Concurrency = 10);
});
```

The first argument is your **module code** — used to derive queue / topic names so multiple modules can share a broker without colliding.

### Modes

- `OperationMode.Dispatch` — the host can start and cancel operations but does not execute them.
- `OperationMode.ConsumeAndDispatch` — the host both starts operations and processes their tasks.

A typical layout: API hosts run `Dispatch`; worker hosts run `ConsumeAndDispatch`.

### Transports

- `OperationTransport.InMemory` — single-process, useful for tests.
- `OperationTransport.ServiceBus` — Azure Service Bus; set `Settings.ConnectionString`.

## Persistence

The default is **in-memory**. To persist sagas across process restarts, install the companion EF Core package:

```
dotnet add package Mpt.Framework.Operation.EFCore
```

Then:

```csharp
services.AddOperation("billing", ops =>
{
    ops.Settings.Mode = OperationMode.ConsumeAndDispatch;
    ops.UseSqlServerPersistence(configuration.Sql.ConnectionString);
    ops.Register<InvoiceBatchOperation>("invoice.batch");
});
```

…and call `modelBuilder.AddOperationEntity()` in your primary DbContext's `OnModelCreating`.

## Defining and dispatching operations

See the [Mpt.Framework.Operation.Abstractions README](https://www.nuget.org/packages/Mpt.Framework.Operation.Abstractions) for the `Operation<,>` programming model and `IOperationDispatcher` usage — those types live in the abstractions package because they should be reachable from your Application layer without pulling the engine in.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
