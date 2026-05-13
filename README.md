# Mpt.Framework — a batteries-included platform-services framework for .NET

[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=softwareone-platform_mpt-framework-net&metric=alert_status)](https://sonarcloud.io/project/overview?id=softwareone-platform_mpt-framework-net)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=softwareone-platform_mpt-framework-net&metric=coverage)](https://sonarcloud.io/component_measures?id=softwareone-platform_mpt-framework-net&metric=coverage)
[![NuGet](https://img.shields.io/nuget/v/Mpt.Framework.Abstractions?label=Mpt.Framework.Abstractions)](https://www.nuget.org/packages/Mpt.Framework.Abstractions)
[![License](https://img.shields.io/github/license/softwareone-platform/mpt-framework-net)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Overview

**Mpt.Framework** is the opinionated set of building blocks the SoftwareOne Marketplace uses to ship platform services in .NET. It gives you a coherent answer to the questions every backend hits on day one:

- How do I model entities with stable identity and optimistic concurrency?
- How do I accept HTTP PATCH payloads and tell *omitted* apart from *explicitly null* — and validate them cleanly?
- How do I orchestrate long-running work without rewriting saga plumbing in every service?
- How do I publish events between modules with server-side filtering, and emit lifecycle events automatically when entities change?
- How do I wire **RQL** queries, policy-enforced writes, and lifecycle hooks into a single repository?

Each capability ships as its own NuGet package so you can adopt one piece without buying the whole stack. Domain code talks to small `*.Abstractions` packages; infrastructure choices (MassTransit, EF Core, Azure Service Bus, FluentValidation) live in the implementation packages so they never leak into your application layer.

## Packages

The family follows a consistent layout: every capability has an **Abstractions** package (interfaces, POCOs, zero infra deps), a **main** package (the engine), and — where it makes sense — an **EFCore** add-on.

| Package | What it does |
| --- | --- |
| [`Mpt.Framework.Abstractions`](src/abstractions/Mpt.Framework.Abstractions/README.md) | Foundational identity & versioning contracts: `IPlatformObject`, `IPlatformEntity`, `IRevisable`. Every other package references this. |
| [`Mpt.Framework.Delta`](src/delta/Mpt.Framework.Delta/README.md) | Strongly-typed JSON partial-update model. `Delta<T>` and `DeltaBuilder` distinguish *absent* from *explicitly null* so HTTP PATCH actually behaves like PATCH. Includes ASP.NET Core binding. |
| [`Mpt.Framework.Delta.Validation`](src/delta/Mpt.Framework.Delta.Validation/README.md) | FluentValidation integration for `Delta<T>`. `DeltaValidator<T>`, `RuleForDelta`, `MustBeDefined`, `WhenDefined`, with JSON-path-aware error messages. |
| [`Mpt.Framework.Operation.Abstractions`](src/operation/Mpt.Framework.Operation.Abstractions/README.md) | Contracts for long-running async work: `Operation<TContract, TTask>`, `IOperationDispatcher`, context interfaces, result POCOs. No MassTransit or EF Core dependency. |
| [`Mpt.Framework.Operation`](src/operation/Mpt.Framework.Operation/README.md) | MassTransit-based operation engine. Start → produce tasks → process in parallel → finish, with in-memory and Azure Service Bus transports out of the box. |
| [`Mpt.Framework.Operation.EFCore`](src/operation/Mpt.Framework.Operation.EFCore/README.md) | SQL Server saga persistence for the Operation engine via EF Core, with optimistic concurrency and a fluent `UseSqlServerPersistence()` helper. |
| [`Mpt.Framework.MessageHub.Abstractions`](src/messagehub/Mpt.Framework.MessageHub.Abstractions/README.md) | Pub/sub contracts: `EventMessage`, `IMessageHubPublisher`, `InputStreamProvider`, `InputStreamFilter`. Use these from your application/domain layer. |
| [`Mpt.Framework.MessageHub`](src/messagehub/Mpt.Framework.MessageHub/README.md) | Module-to-module event bus on MassTransit. Server-side SQL filtering on Azure Service Bus, in-memory transport for tests, optional `IPlatformEventEmitter` for lifecycle events. |
| [`Mpt.Framework.Mapping.Abstractions`](src/mapping/Mpt.Framework.Mapping.Abstractions/README.md) | Mapper contracts: `IDynamicEntityMapper`, `IInMemoryEntityMapper`. Application layers depend on these without referencing RQL. |
| [`Mpt.Framework.Mapping`](src/mapping/Mpt.Framework.Mapping/README.md) | Reflection-driven, RQL-aware mapper that updates persistence entities in place from view models — collections, nested objects, references — and reports the changed-property count. |
| [`Mpt.Framework.Mapping.EFCore`](src/mapping/Mpt.Framework.Mapping.EFCore/README.md) | EF Core flavour of the mapper: looks up platform-entity references through the `DbContext` (FK assignment, navigation loading, removal tracking). |
| [`Mpt.Framework.Persistence.Abstractions`](src/persistence/Mpt.Framework.Persistence.Abstractions/README.md) | Repository / unit-of-work / query / policy contracts and lifecycle-hook interfaces. Pure abstractions — no EF Core, no RQL. |
| [`Mpt.Framework.Persistence`](src/persistence/Mpt.Framework.Persistence/README.md) | The repository engine: RQL-driven reads, policy-enforced writes, lifecycle hooks (`OnCreatingAsync`, `OnUpdatingAsync`, `OnDeletingAsync`), and automatic after-save events through MessageHub. |
| [`Mpt.Framework.Persistence.EFCore`](src/persistence/Mpt.Framework.Persistence.EFCore/README.md) | EF Core flavour: `EfCoreRepository<TDbEntity, TEntity>` over a user-supplied `DbContext`, transactional saves, composed with the EF Core mapper. |

## Highlights

- **Clean architecture by default.** Abstractions packages let domain code depend on interfaces; MassTransit, EF Core and Azure Service Bus stay quarantined in implementation packages.
- **PATCH that actually patches.** `Delta<T>` preserves which fields the client sent, so updates do not accidentally null out unspecified properties.
- **RQL-native.** Reads and mappings are powered by [Mpt.Rql](https://github.com/softwareone/mpt-rql-net), so filtering, sorting, paging and projections come for free.
- **Eventing without ceremony.** A repository save automatically emits `GenericCreatedEvent<TEntity>` / `GenericUpdatedEvent<TEntity>` / `GenericDeletedEvent<TEntity>` through MessageHub — opt in per entity, no boilerplate.
- **Long-running work that survives restarts.** Operations are MassTransit sagas; pair `Mpt.Framework.Operation` with `Mpt.Framework.Operation.EFCore` for durable SQL Server state.
- **In-memory variants for tests.** Every package that talks to infrastructure ships an in-memory implementation so unit tests stay fast.

## Quick start

### Install the pieces you need

You rarely install everything at once. Most services start with Persistence, MessageHub, and Delta:

```bash
dotnet add package Mpt.Framework.Persistence.EFCore
dotnet add package Mpt.Framework.MessageHub
dotnet add package Mpt.Framework.Delta
dotnet add package Mpt.Framework.Delta.Validation
```

### Define an entity that participates in the framework

```csharp
using Mpt.Framework;

public sealed class Invoice : IPlatformEntity
{
    public string Id { get; set; } = default!;
    public int Revision { get; set; }

    public string Number { get; set; } = default!;
    public decimal Amount { get; set; }
}
```

### Wire it up

```csharp
// Program.cs
builder.Services.AddRql();
builder.Services.AddMessageHub(opts => opts.UseInMemoryTransport());
builder.Services.AddPersistence()
    .AddEntity<InvoiceDbEntity, Invoice>(entity =>
    {
        entity.Configure<InvoiceConfiguration>();
    });
builder.Services.AddDbContext<AppDbContext>(/* ... */);
```

### Accept a PATCH with proper semantics

```csharp
public sealed class UpdateInvoice
{
    public Delta<string> Number { get; set; }
    public Delta<decimal> Amount { get; set; }
}

public sealed class UpdateInvoiceValidator : DeltaValidator<UpdateInvoice>
{
    public UpdateInvoiceValidator()
    {
        RuleForDelta(x => x.Number).WhenDefined(rule => rule.NotEmpty());
        RuleForDelta(x => x.Amount).WhenDefined(rule => rule.GreaterThan(0));
    }
}
```

Once the request lands, the repository, mapper, lifecycle hooks and event emitter cooperate to apply only the fields the client actually sent, run the configured update policy, bump the `Revision`, and publish an `InvoiceUpdated` event downstream.

For end-to-end recipes — including operation orchestration, EF Core composition, and custom event producers — see the per-package READMEs linked in the table above.

## Building from source

Requires **.NET 10 SDK** (10.0.x).

```bash
git clone https://github.com/softwareone/mpt-framework-net.git
cd mpt-framework-net

dotnet restore Mpt.Framework.slnx
dotnet build Mpt.Framework.slnx --no-restore --configuration Release
dotnet test  Mpt.Framework.slnx --no-build  --configuration Release \
    --results-directory ./TestResults/ \
    --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

The same three commands run in CI on every push and pull request — see `.github/workflows/sonar.yaml`. Coverage is reported in OpenCover format and uploaded to SonarCloud.

## Project layout

```
src/
  abstractions/   Mpt.Framework.Abstractions
  delta/          Mpt.Framework.Delta, Mpt.Framework.Delta.Validation
  operation/      Mpt.Framework.Operation{,.Abstractions,.EFCore}
  messagehub/     Mpt.Framework.MessageHub{,.Abstractions}
  mapping/        Mpt.Framework.Mapping{,.Abstractions,.EFCore}
  persistence/    Mpt.Framework.Persistence{,.Abstractions,.EFCore}
tests/
  <one folder per family, mirroring src/>
```

The solution file is `Mpt.Framework.slnx` (the new XML solution format introduced in .NET 9).

## Contributing

We welcome contributions to enhance the framework. To get started:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/your-capability`)
3. Commit your changes (`git commit -m 'Add your capability'`)
4. Push to the branch (`git push origin feature/your-capability`)
5. Open a Pull Request

Please run `dotnet test` locally before opening the PR; CI runs the same suite on Ubuntu under .NET 10.

## License

This project is licensed under the Apache License 2.0 — see the [`LICENSE`](LICENSE) file for details.

## Acknowledgements

- The SoftwareOne Marketplace team for creating and maintaining this framework
- All contributors who have helped improve it

---

Developed by the SWO Marketplace team
