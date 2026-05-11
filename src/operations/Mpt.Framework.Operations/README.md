# Mpt.Framework.Operations

The MassTransit-based operations engine. Orchestrates long-running asynchronous work via a saga, dispatching task batches over an in-memory or Azure Service Bus transport.

Each operation is defined as a single class that:

1. **Starts** — optionally checks a start condition that can postpone the operation.
2. **Produces tasks** — emits a stream of task payloads from `IAsyncEnumerable`.
3. **Processes each task** — runs once per emitted payload, in parallel up to the configured concurrency.
4. **Finishes** — receives an aggregated result (total / succeeded / failed / cancelled).

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Operations
```

In your Application layer, depend on the lightweight [`Mpt.Framework.Operations.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Operations.Abstractions) instead — that gives you the `Operation<,>` base class and `IOperationDispatcher` without pulling MassTransit / EF Core into your domain projects.

For durable SQL Server persistence, add the companion package [`Mpt.Framework.Operations.EFCore`](https://www.nuget.org/packages/Mpt.Framework.Operations.EFCore). This package on its own ships with **in-memory persistence**, which is fine for tests and prototypes.

Target framework: **net10.0**.

## Registration

```csharp
using Mpt.Framework.Operations;
using Mpt.Framework.Operations.Configuration;

services.AddOperations("billing", ops =>
{
    ops.Settings.Mode = OperationsMode.ConsumeAndDispatch;
    ops.Settings.Transport = OperationsTransport.InMemory; // or OperationsTransport.ServiceBus
    ops.Register<InvoiceBatchOperation>("invoice.batch", t => t.Tasks.Concurrency = 10);
});
```

The first argument is your **module code** — used to derive queue / topic names so multiple modules can share a broker without colliding.

### Modes

- `OperationsMode.Dispatch` — the host can start and cancel operations but does not execute them.
- `OperationsMode.ConsumeAndDispatch` — the host both starts operations and processes their tasks.

A typical layout: API hosts run `Dispatch`; worker hosts run `ConsumeAndDispatch`.

### Transports

- `OperationsTransport.InMemory` — single-process, useful for tests.
- `OperationsTransport.ServiceBus` — Azure Service Bus; set `Settings.ConnectionString`.

## Persistence

The default is **in-memory**. To persist sagas across process restarts, install the companion EF Core package:

```
dotnet add package Mpt.Framework.Operations.EFCore
```

Then:

```csharp
services.AddOperations("billing", ops =>
{
    ops.Settings.Mode = OperationsMode.ConsumeAndDispatch;
    ops.UseSqlServerPersistence(configuration.Sql.ConnectionString);
    ops.Register<InvoiceBatchOperation>("invoice.batch");
});
```

…and call `modelBuilder.AddOperationsEntity()` in your primary DbContext's `OnModelCreating`.

## Defining and dispatching operations

See the [Mpt.Framework.Operations.Abstractions README](https://www.nuget.org/packages/Mpt.Framework.Operations.Abstractions) for the `Operation<,>` programming model and `IOperationDispatcher` usage — those types live in the abstractions package because they should be reachable from your Application layer without pulling the engine in.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
