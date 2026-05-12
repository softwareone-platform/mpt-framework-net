# Mpt.Framework.Mapping.EFCore

Entity Framework Core flavour of the [Mpt.Framework.Mapping](https://www.nuget.org/packages/Mpt.Framework.Mapping) dynamic entity mapper.

The in-memory mapper that ships in `Mpt.Framework.Mapping` walks every nested object recursively. That's the right behaviour for view-models in tests, but wrong against an EF Core graph: navigation references should be reassigned by id (so EF doesn't try to insert a new aggregate) and removed collection items should go through the change tracker so they get deleted on `SaveChanges`.

`EfCoreDynamicEntityMapper` overrides four extension points on `DynamicEntityMapper` to plug into EF Core:

- `UseAssignForPlatformEntities` returns `true` — `IPlatformObject` references are reassigned by id rather than deep-copied.
- `FindEntityAsync` calls `DbContext.FindAsync(Type, id)`.
- `EnsureCollectionLoadedAsync` calls `EntityEntry.Collection(name).LoadAsync()`, with special-cases for owned types and skip navigations.
- `UpdatePlatformEntityReference` stamps the FK on the owner entry (or on the dependent side, for reverse references) so the change tracker emits one `UPDATE` instead of an `INSERT`.
- `EnsureEntityRemovedAsync` calls `DbContext.Remove(entity)` so removed collection items are deleted on `SaveChanges`.

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Mapping.EFCore
```

In your Application layer, depend on the lightweight [`Mpt.Framework.Mapping.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Mapping.Abstractions) — your services should take `IDynamicEntityMapper` or `IEfCoreDynamicEntityMapper`, not the concrete class.

Target framework: **net10.0**.

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;

services.AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString));
services.AddRql(c => c.ScanForMappers(typeof(InvoiceViewMap).Assembly));
services.AddEfCoreMapping<MyDbContext>();   // IDynamicEntityMapper / IEfCoreDynamicEntityMapper
```

`AddEfCoreMapping<TDbContext>()` registers the mapper as `IDynamicEntityMapper`, `IEfCoreDynamicEntityMapper`, and the concrete `EfCoreDynamicEntityMapper` — all scoped, so the mapper shares the DbContext's scope.

## Usage

```csharp
public sealed class UpdateInvoiceHandler(IEfCoreDynamicEntityMapper mapper, MyDbContext db)
{
    public async Task<int> ApplyAsync(string id, InvoiceView view, CancellationToken cancellationToken)
    {
        var entity = await db.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice '{id}' not found.");

        var changed = await mapper.MapAsync(view, entity);

        if (changed > 0)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
```

What the mapper does in this example:

- Primitive properties (`Amount`, `Status`, `IssuedOn`, …) are copied straight across.
- `LineItems` is matched item-by-item by `IPlatformObject.Id` — existing rows are updated in place, new ones are inserted, removed ones are tracked for deletion.
- `Customer` (an `IPlatformObject`) is reassigned by id: the FK on the invoice is updated to point at the new customer, without EF trying to insert a new customer.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
