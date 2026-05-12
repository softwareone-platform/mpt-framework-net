# Mpt.Framework.Persistence

Query + Rules + Data orchestration for `Mpt.Framework.*`. Combines:

- **Read surface** — an RQL-driven `IQueryService<TEntity>` with paging, custom filters, RQL-shaped projections.
- **Write surface** — `IRepository<TEntity>` + `IUnitOfWork` that batch Add / Delete / GetForUpdate work and flush on `SaveChangesAsync`.
- **Rules surface** — declarative `IEntityConfiguration<TEntity>` with role-aware action and property policies; per-entity lifecycle hooks; per-entity event producers wired through `IMessageHubPublisher`.

The `Repository<TEntity>` abstract base is the glue: it walks Add/Update/Delete sets through the four-phase save flow (`OnSaveChangesInitiated` → `OnBeforeSaveChanges` → persistence write → `OnAfterSaveChanges`), invoking the configured lifecycle hooks before the write and emitting events through MessageHub after.

This package ships with an in-memory query strategy and an `InMemoryUnitOfWork` suitable for tests. For SQL Server persistence install [`Mpt.Framework.Persistence.EFCore`](https://www.nuget.org/packages/Mpt.Framework.Persistence.EFCore) and call `.AddEfCorePersistence<TDbContext>()` inside the `AddPersistence` callback.

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Persistence
```

In your Application layer, depend on the lightweight [`Mpt.Framework.Persistence.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Persistence.Abstractions) — it covers every interface and POCO consumers should hold references to.

Target framework: **net10.0**.

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;

services.AddDbContext<MyDbContext>(o => o.UseSqlServer(connectionString));
services.AddRql(c => c.ScanForMappers(typeof(InvoiceMap).Assembly));
services.AddInMemoryMapping();           // change-tracking clones
services.AddEfCoreMapping<MyDbContext>(); // the DB-side dynamic mapper

services.AddPersistence("invoices", builder =>
{
    builder.AddEfCorePersistence<MyDbContext>();
    builder.AddEntity<InvoiceDbEntity, Invoice, InvoiceQueryService>();
    builder.ScanForConfigurations(typeof(InvoiceConfig).Assembly);
});

// Optional — wire MessageHub so the engine can publish lifecycle events.
services.AddMessageHub("invoices", hub => { /* … streams, transport … */ });
```

## Repository lifecycle

```csharp
public sealed class CreateInvoiceHandler(IUnitOfWork uow)
{
    public async Task ExecuteAsync(Invoice draft, CancellationToken cancellationToken)
    {
        var repo = uow.GetRepository<Invoice>();
        repo.Add(draft);

        // OnCreatingAsync fires for the draft; if MessageHub is registered, the
        // configured IEntityEventProducer's events are published after SaveChanges.
        await uow.SaveChangesAsync(cancellationToken);
    }
}

public sealed class UpdateInvoiceHandler(IUnitOfWork uow)
{
    public async Task ExecuteAsync(string id, decimal newTotal, CancellationToken cancellationToken)
    {
        var repo = uow.GetRepository<Invoice>();
        var invoice = await repo.GetForUpdateOrThrowAsync(id, cancellationToken);
        invoice.Total = newTotal;

        // OnUpdatingAsync receives the original snapshot via IEntityUpdatingContext.Original.
        await uow.SaveChangesAsync(cancellationToken);
    }
}
```

## Customising per-entity behaviour

Three open-generic hooks are registered by default. Override any of them with a closed implementation in your domain assembly and `ScanForConfigurations` will pick it up:

- `EntityConfiguration<TEntity>` — declare action + update policies (see `Mpt.Framework.Persistence.Abstractions` README for the DSL surface).
- `EntityLifecycleHooks<TEntity>` — `OnCreatingAsync` / `OnUpdatingAsync` / `OnDeletingAsync`. Use to enforce invariants or compute derived properties.
- `EntityEventProducer<TEntity>` — return `EventMessage` instances; the engine publishes them via the registered `IMessageHubPublisher`.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
