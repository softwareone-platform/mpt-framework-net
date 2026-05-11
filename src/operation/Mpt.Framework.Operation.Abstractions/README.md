# Mpt.Framework.Operation.Abstractions

Pure abstractions for the [Mpt.Framework.Operation](https://www.nuget.org/packages/Mpt.Framework.Operation) engine. Contains only interfaces and POCOs — **no third-party runtime dependencies**, no MassTransit, no EF Core.

## When to install

Reference this package from your **Application layer** (or any project that needs to *define* operations or *dispatch* them, but should not depend on the engine itself):

```
dotnet add package Mpt.Framework.Operation.Abstractions
```

Install the full engine package `Mpt.Framework.Operation` only in your **composition root** (Worker / API host).

This keeps Clean Architecture intact: your domain and application code don't take a transitive dependency on MassTransit, Azure Service Bus, or EF Core.

## What's inside

| Type                                  | Purpose                                                       |
| ------------------------------------- | ------------------------------------------------------------- |
| `Operation<TContract, TTask>`         | Abstract base class — derive to define an operation.          |
| `IOperationContract`                  | Marker interface for the operation's input contract.          |
| `IOperationDispatcher`                | API-side interface for starting and cancelling operations.    |
| `IOperationStartingContext<T>`        | Passed to `OnStartingAsync` (lets you postpone the start).    |
| `IOperationPreparingContext<T>`       | Passed to `GetTasksAsync`.                                    |
| `IProcessTaskContext<T>`              | Passed to `ProcessTaskAsync`.                                 |
| `IOperationFinishedContext<T>`        | Passed to `OnFinishedAsync` — carries the final `OperationResult`. |
| `TaskResult`                          | `Success` / `Failure` enum returned from `ProcessTaskAsync`.  |
| `OperationResult`, `OperationStatus`, `OperationStatistics`, `OperationFailure`, `OperationMetadata`, `TaskMetadata` | Result and metadata POCOs. |

## Defining an operation (Application layer)

```csharp
using Mpt.Framework.Operation;

public class InvoiceBatchOperation : Operation<InvoiceBatchOperation.OperationData, InvoiceBatchOperation.TaskData>
{
    public override IAsyncEnumerable<TaskData> GetTasksAsync(IOperationPreparingContext<OperationData> context, CancellationToken cancellationToken)
    {
        /* ... */
    }

    public override Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<TaskData> context, CancellationToken cancellationToken)
    {
        /* ... */
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

## Dispatching (e.g. API controller)

```csharp
public sealed class InvoiceController(IOperationDispatcher dispatcher)
{
    [HttpPost]
    public async Task<IActionResult> Run(Guid monthId, CancellationToken cancellationToken)
    {
        var operationId = await dispatcher.DispatchAsync(
            new InvoiceBatchOperation.OperationData { MonthId = monthId },
            cancellationToken);
        return Accepted(new { operationId });
    }
}
```

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
