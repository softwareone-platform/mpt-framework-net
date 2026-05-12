# Mpt.Framework.Persistence.EFCore

Entity Framework Core flavour of [`Mpt.Framework.Persistence`](https://www.nuget.org/packages/Mpt.Framework.Persistence). Provides:

- `EfCoreRepository<TDbEntity, TEntity>` — `Repository<TEntity>` subclass that drives EF Core's change tracker. Uses [`Mpt.Framework.Mapping.EFCore`](https://www.nuget.org/packages/Mpt.Framework.Mapping.EFCore)'s dynamic mapper to project between view-model and DB-side entity on insert / update.
- `EfCoreUnitOfWork` — wraps the save-flow in a single `DbContext` transaction when the provider supports one (skipped automatically for the InMemory provider used in tests).
- `EfCoreQueryExecutionStrategy` — delegates async query operations to EF Core's async extension methods so reads execute against the database asynchronously.
- `services.AddEfCorePersistence<TDbContext>()` — the registration extension that swaps the in-memory defaults for the EF Core ones.

## Install

Install this package only in your **composition root** (Worker / API host):

```
dotnet add package Mpt.Framework.Persistence.EFCore
```

In your Application layer, keep depending on [`Mpt.Framework.Persistence.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Persistence.Abstractions) — handlers and entity definitions don't see this package.

Target framework: **net10.0**.

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Mpt.Rql;

services.AddDbContext<MyDbContext>(o => o.UseSqlServer(connectionString));
services.AddRql(c => c.ScanForMappers(typeof(InvoiceMap).Assembly));
services.AddInMemoryMapping();             // change-tracking clones
services.AddEfCoreMapping<MyDbContext>();  // dynamic mapper for EF Core

services.AddPersistence("invoices", builder =>
{
    builder.AddEfCorePersistence<MyDbContext>();
    builder.AddEntity<InvoiceDbEntity, Invoice, InvoiceQueryService>();
    builder.ScanForConfigurations(typeof(InvoiceConfig).Assembly);
});
```

`AddEfCorePersistence<TDbContext>` does three things:

1. Registers `DbContext` as a scoped alias for `TDbContext` (so the repository's `DbContext`-typed constructor parameter resolves).
2. Substitutes `EfCoreQueryExecutionStrategy` for the in-memory default.
3. Substitutes `EfCoreUnitOfWork` for `InMemoryUnitOfWork` and configures the `PersistenceBuilder` to register `EfCoreRepository<TDbEntity, TEntity>` whenever you call `.AddEntity<TDbEntity, TEntity, TQueryService>()`.

## Per-entity wiring

For every entity pair, you need:

- A `TDbEntity` mapped in your DbContext.
- A view-model `TEntity` implementing `IPlatformEntity` + `IRqlGraphHolder`.
- An RQL mapper (`IRqlMapper<TDbEntity, TEntity>`) describing the projection.
- A `QueryService<TDbEntity, TEntity>` subclass that overrides `GetQuery`, `GetByIdPredicate`, and exposes the EF Core execution strategy.

The repository (`EfCoreRepository<TDbEntity, TEntity>`) is constructed automatically per the `RepositoryTypeResolver` configured by `AddEfCorePersistence`.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
