# Mpt.Framework.Persistence.Abstractions

Pure abstractions for the [Mpt.Framework.Persistence](https://www.nuget.org/packages/Mpt.Framework.Persistence) engine. Reference this package from your **Application layer** — define entities, configurations, lifecycle hooks, and event producers without pulling EF Core or RQL's full surface into your domain projects.

## What's inside

| Type                                        | Purpose                                                                                                |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `IRepository<TEntity>`                      | Per-entity read + tracked-write surface (Add / Delete / GetForUpdate / List / Count).                  |
| `IUnitOfWork`                               | Aggregates repositories; drives the four-phase save flow on `SaveChangesAsync`.                        |
| `IQueryService<TEntity>` / `IQueryService`  | Read-only query surface backed by RQL.                                                                 |
| `IGetEntityOptions` / `IGetEntityListOptions<TEntity>` / `IListOrderOptions<TEntity>` | Fluent options for single / list reads.            |
| `DataPage<T>` / `DataPageRequest`           | Paged-list result and request.                                                                         |
| `IFilterProvider<TDbEntity>`                | Custom-filter extensibility point applied during `GetPageAsync`.                                       |
| `IEntityConfiguration<TEntity>`             | Declarative policy surface — `ConfigureActions` / `ConfigureUpdate`.                                   |
| `IEntityLifecycleHooks<TEntity>`            | `OnCreatingAsync` / `OnUpdatingAsync` / `OnDeletingAsync`.                                             |
| `IEntityEventProducer<TEntity>`             | `ProduceCreatedEvents` / `ProduceUpdatedEvents` / `ProduceStatusChangedEvents` / `ProduceDeletedEvents` / `RegisterCustomEvent` + `ProduceCustomEvents` / `CustomizeEvents` / `Reset`. Registers `Generic*Event<TEntity>` (and `CustomEvent<TEntity>`) with `Mpt.Framework.MessageHub.IPlatformEventEmitter`; the unit of work flushes the emitter after every repository produces. |
| `ISyncPlatformEventProducer<TEntity>`       | Forward-compat marker for a sync-stream pipeline; no concrete implementation ships.                    |
| Policy DSL                                  | `IActionPolicy`, `IEventPolicy`, `IUpdatePolicy`, `IUpdatePolicyProperty`, `IUpdatePolicyRuleBuilder`. |
| `EntityAction` / `EntityEventTypes` / `PolicyRuleAccess` / `PropertyHints` | Enums.                                                            |
| `IRqlGraphHolder`                           | Marker that view-models carry an `IRqlNode` graph.                                                     |
| `PersistenceEntityNotFoundException`        | Thrown from `GetOrThrow` overloads.                                                                    |

## Cooperating packages

- [`Mpt.Framework.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.Abstractions) — `IPlatformObject`, `IPlatformEntity`, `IRevisable` (entities must implement `IPlatformEntity`).
- [`Mpt.Framework.Delta`](https://www.nuget.org/packages/Mpt.Framework.Delta) — `Delta<T>` consumed by the update-policy DSL.
- [`Mpt.Framework.MessageHub.Abstractions`](https://www.nuget.org/packages/Mpt.Framework.MessageHub.Abstractions) — `IMessageHubPublisher` is the seam the engine emits events through.
- [`Mpt.Rql`](https://www.nuget.org/packages/Mpt.Rql) — `RqlRequest`, `IRqlSettings`, `IRqlNode`.

## Example

```csharp
using Mpt.Framework;
using Mpt.Framework.Persistence;
using Mpt.Rql;

public class Invoice : IPlatformEntity, IRqlGraphHolder
{
    public string Id { get; set; } = string.Empty;
    public int Revision { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public IRqlNode? RqlGraph { get; set; }
}

public sealed class InvoiceConfig : EntityConfiguration<Invoice>
{
    protected override void ConfigureActions(IActionPolicy<Invoice> policy)
    {
        policy.Define(EntityAction.Create, "ops");
        policy.Define(EntityAction.Update, "ops", "client");
        policy.Define(EntityAction.Delete, "ops");
    }

    protected override void ConfigureUpdate(IUpdatePolicy<Invoice> policy)
    {
        policy.Property(i => i.Status, p => p.Allow("ops"));
        policy.Property(i => i.Total, p => p.Allow("ops"));
    }
}
```

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
