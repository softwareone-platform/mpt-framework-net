# Mpt.Framework.Mapping

[Mpt.Rql](https://www.nuget.org/packages/Mpt.Rql)-driven dynamic entity-to-DTO mapping engine.

`IDynamicEntityMapper` updates a persistence entity in place from a view-model instance, walking nested objects and collections in the same shape the projection produced. It returns the number of properties that actually changed, so you can decide whether to save.

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Mapping
```

In your Application layer, depend on the lightweight [`Mpt.Framework.Mapping.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Mapping.Abstractions) instead — that gives you the mapper interfaces without pulling `Mpt.Rql` into your domain projects.

Target framework: **net10.0**.

## Registration

The engine sits on top of [Mpt.Rql](https://www.nuget.org/packages/Mpt.Rql) — register it first.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;

services.AddRql(c => c.ScanForMappers(typeof(InvoiceViewMap).Assembly));
services.AddInMemoryMapping();   // IDynamicEntityMapper / IInMemoryEntityMapper
```

## Updating an entity from a view-model

```csharp
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

For partial updates, call `MapPathAsync` with an expression naming the property you want to patch.

### Platform objects and platform entities

Two interfaces from [`Mpt.Framework.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Abstractions) drive identity-aware mapping:

- `IPlatformObject` — single `string Id` property. The mapper uses this to match collection items by id, so existing instances are updated in place instead of being replaced.
- `IPlatformEntity` — `IPlatformObject` + `IRevisable`. Distinguishes a first-class, revisable identifiable from a value-shaped nested object. When a subclassed mapper opts into reference-assignment (`UseAssignForPlatformEntities = true`), platform-entity collections become "lookup by id and assign" rather than walked-and-copied.

Nested platform-entity properties (a chain of more than one `.Foo.Bar` where the leaf type is `IPlatformEntity`) are intentionally not supported by the mapper — model these as id references and resolve them in the loader instead.

## Extending the mapper

Subclass [`DynamicEntityMapper`](DynamicEntityMapper.cs) when you need persistence-aware behaviour — for example to look up platform-entity references from a database, or to mark collections as loaded before the mapper mutates them. Override:

- `UseAssignForPlatformEntities` — when `true`, platform-entity references are reassigned by id (via `UpdatePlatformEntityReference`) instead of deep-copied.
- `FindEntityAsync(Type, object)` — look up an existing persistence entity for a source platform object.
- `EnsureCollectionLoadedAsync(object, PropertyInfo)` — ensure a navigation collection is materialised before the mapper mutates it.
- `EnsureEntityRemovedAsync(object)` — fired when the mapper removes an entry from a platform-object collection.
- `UpdatePlatformEntityReference(object, PropertyInfo, object?)` — reassign a reference-typed property by id.

The shipped [`InMemoryEntityMapper`](InMemoryEntityMapper.cs) returns the "no-op" answers for each so that view-model graphs can be mapped without any database integration.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
