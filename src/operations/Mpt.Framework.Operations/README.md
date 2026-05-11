# Mpt.Framework.Operations

Operations engine for orchestrating long-running asynchronous work over [MassTransit](https://masstransit.io/). Each operation is defined as a single class that:

1. **Starts** — optionally checks a start condition that can defer the operation until later.
2. **Produces tasks** — emits a stream of task payloads from `IAsyncEnumerable`.
3. **Processes each task** — runs once per emitted payload, in parallel up to the configured concurrency.
4. **Finishes** — receives an aggregated result (total / succeeded / failed / cancelled).

The framework persists progress in a saga, dispatches tasks via a message bus, and reports completion back to your code.

## Install

```
dotnet add package Mpt.Framework.Operations
```

Target framework: **net10.0**. For durable SQL Server persistence, add the companion package [`Mpt.Framework.Operations.EntityFrameworkCore`](https://www.nuget.org/packages/Mpt.Framework.Operations.EntityFrameworkCore). The main package on its own ships with **in-memory persistence**, which is fine for tests and prototypes.

## Defining an operation

```csharp
using Mpt.Framework.Operations;

internal class InvoiceBatchOperation : Operation<InvoiceBatchOperation.OperationData, InvoiceBatchOperation.TaskData>
{
    public override async IAsyncEnumerable<TaskData> GetTasksAsync(
        IOperationPreparingContext<OperationData> context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var invoiceId in await LoadInvoiceIdsAsync(context.Operation.MonthId, cancellationToken))
        {
            yield return new TaskData { InvoiceId = invoiceId };
        }
    }

    public override async Task<TaskResult> ProcessTaskAsync(
        IProcessTaskContext<TaskData> context,
        CancellationToken cancellationToken)
    {
        var ok = await GenerateInvoiceAsync(context.Task.InvoiceId, cancellationToken);
        return ok ? TaskResult.Success : TaskResult.Failure;
    }

    public override Task OnFinishedAsync(IOperationFinishedContext<OperationData> context, CancellationToken cancellationToken)
    {
        // context.Result.Status / Statistics / Failure
        return Task.CompletedTask;
    }

    public class OperationData : IOperationContract
    {
        public required Guid MonthId { get; init; }
    }

    public class TaskData
    {
        public required Guid InvoiceId { get; init; }
    }
}
```

## Registration

```csharp
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

## Dispatching

Inject `IOperationDispatcher` and call `DispatchAsync`:

```csharp
public sealed class InvoiceController(IOperationDispatcher dispatcher)
{
    public async Task<IActionResult> Run(Guid monthId, CancellationToken cancellationToken)
    {
        var operationId = await dispatcher.DispatchAsync(
            new InvoiceBatchOperation.OperationData { MonthId = monthId },
            cancellationToken);

        return Accepted(new { operationId });
    }
}
```

`DispatchAsync` returns the operation id; you can later `CancelAsync<TContract>(id, ct)` to stop processing.

## Persistence

The main package only knows in-memory persistence. To persist sagas across process restarts, install the companion EF Core package:

```
dotnet add package Mpt.Framework.Operations.EntityFrameworkCore
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

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
