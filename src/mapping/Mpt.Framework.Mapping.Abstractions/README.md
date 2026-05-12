# Mpt.Framework.Mapping.Abstractions

Pure abstractions for the [Mpt.Framework.Mapping](https://www.nuget.org/packages/Mpt.Framework.Mapping) engine. Contains only interfaces and POCOs — **no third-party runtime dependencies**, no RQL, no DI container.

## When to install

Reference this package from your **Application layer** (or any project that needs to define services that consume the dynamic mapper, but should not depend on the engine itself):

```
dotnet add package Mpt.Framework.Mapping.Abstractions
```

Install the full engine package `Mpt.Framework.Mapping` only in your **composition root** (Worker / API host).

This keeps Clean Architecture intact: your domain and application code don't take a transitive dependency on `Mpt.Rql` or the DI container.

## What's inside

| Type                                       | Purpose                                                                                  |
| ------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `IDynamicEntityMapper`                     | RQL-driven mapper that updates a persistence entity in place from a view model.          |
| `IInMemoryEntityMapper`                    | Marker variant resolved when callers want in-memory mapping behaviour.                   |

Identity matching during mapping is driven by `IPlatformObject` and `IPlatformEntity` from [`Mpt.Framework.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Abstractions) (referenced transitively).

## Defining a service that uses the mapper

```csharp
using Mpt.Framework.Mapping;

public sealed class UpdateInvoiceHandler(IInMemoryEntityMapper mapper, IMyDb db)
{
    public async Task<int> ApplyAsync(string id, InvoiceView view, CancellationToken cancellationToken)
    {
        var entity = await db.Invoices.FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice '{id}' not found.");

        var changed = await mapper.MapAsync(view, entity);

        if (changed > 0)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
```

The composition root registers the mapper with `services.AddInMemoryMapping()` after `services.AddRql(...)` (see `Mpt.Framework.Mapping`).

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
