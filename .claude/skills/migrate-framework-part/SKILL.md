---
name: migrate-framework-part
description: Use when migrating or syncing code from the private mpt-library-framework into this OSS mpt-framework-net repo — extracting a new component (delta, operation, messagehub, etc.) or propagating later upstream changes. Covers package layout (Abstractions / main / EFCore split), naming (singular nouns, EFCore suffix, Apache-2.0), standard dependency substitutions, what to skip from upstream (Models.Shared, business taxonomy, platform-entity-coupled code), and verification.
---

# Migrate a framework part into mpt-framework-net

A precedent-driven guide. Components migrated so far: **Delta** (single package), **Operation** (Abstractions + main + EFCore), **MessageHub** (Abstractions + main, no EFCore), **Mapping** (Abstractions + main + EFCore, requires `Mpt.Rql` — carved out of upstream `Projections/`), and the cross-cutting **Abstractions** package (`IPlatformObject` / `IPlatformEntity` / `IRevisable`). Use them as worked examples.

## TL;DR

1. **Read** the upstream files. Clone the source repo (`mpt-library-framework`) somewhere; its working tree on the OSS-extraction branch is intentionally empty (uncommitted deletes), so use `git show HEAD:<path>` to read.
2. **Decide the split** — Abstractions only if Application-layer types are exposed; EFCore add-on only if SQL persistence is needed.
3. **Skip business-coupled code** — anything from `Mpt.Framework.Models.Shared`, anything referencing `UserAccountType`, `IPlatformEntity`, `PlatformEntityMap`, the platform-events authoring layer, sync providers.
4. **Replace framework deps** with local equivalents (table below).
5. **Mirror the layout** of an existing component (delta is the simplest reference; operation is the fullest).
6. **Migrate tests too**, then `dotnet build Mpt.Framework.slnx` and `dotnet test`.

---

## Repo layout (precedent)

```
src/<component>/Mpt.Framework.<Component>.Abstractions/   ← interfaces + POCOs, zero third-party deps
src/<component>/Mpt.Framework.<Component>/                ← engine
src/<component>/Mpt.Framework.<Component>.EFCore/         ← optional SQL persistence add-on
tests/<component>/Mpt.Framework.<Component>.Tests/        ← xunit + FluentAssertions
Mpt.Framework.slnx                                         ← slnx solution; no Directory.*.props
```

- `<component>` folder name is **lowercase singular** (`delta`, `operation`, `messagehub`).
- Target framework: **net10.0**.
- No central package management — explicit `[X.Y.Z,W.0.0)` version ranges per csproj.
- License: **Apache-2.0** in every csproj `<PackageLicenseExpression>` and the root `LICENSE`.

## Source repo specifics

- **Repo**: `mpt-library-framework` (private). Clone it wherever you keep your repos; this skill refers to it as "the source clone".
- **Working tree state**: intentionally empty on the OSS-extraction branch (uncommitted deletes are how upstream marks "considered for OSS"). Use `git show HEAD:<path>` to read files; `git ls-tree -r --name-only HEAD | grep -i <component>` to enumerate.
- **Branches**: `master` is the truth; release branches exist (`release/4`, etc.).

## Package decomposition decision tree

```
Is the type meant to be used in Application/domain layer code?
   ├─ Yes → carve Mpt.Framework.<X>.Abstractions (zero third-party deps).
   │        Engine package ProjectReferences it.
   │        Examples: Operation, MessageHub.
   └─ No  → single package is fine.
            Example: Delta (also has Delta.Validation as a separate add-on).

Does the component need durable SQL Server persistence?
   ├─ Yes → carve Mpt.Framework.<X>.EFCore add-on.
   │        Implements a pluggable persistence interface defined in main package
   │        (e.g. IOperationPersistenceProvider).
   │        Main package ships an InMemory default.
   │        Example: Operation.EFCore.
   └─ No  → skip.
            Example: MessageHub (transport only, no DB).

Other natural extension points (FluentValidation, AppInsights, etc.) → separate add-on package.
   Example: Delta.Validation.
```

## Naming conventions

| What | Rule | Example |
|---|---|---|
| Package family name | **Singular noun** | `Mpt.Framework.Operation`, not `.Operations` |
| EF Core add-on suffix | `.EFCore` (abbreviated) | `Mpt.Framework.Operation.EFCore`, not `.EntityFrameworkCore` |
| Abstractions package | `.<Name>.Abstractions`, shares **root namespace** with engine | `Mpt.Framework.Operation.Abstractions` + namespace `Mpt.Framework.Operation` |
| License | Apache-2.0 in every csproj `<PackageLicenseExpression>` | `<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>` |
| Internal namespace collision (e.g. internal `Foo<,,>` shadows public `Foo`) | Rename **internal** to free the name | `OperationBuilder<,,>` (internal) renamed to `OperationRegistration<,,>` so public `OperationBuilder` exists |
| Namespace–type clash (e.g. `Mpt.Framework.Operation.Operation<,>`) | **Acceptable** — same pattern as `Delta.Delta<T>`, `System.Threading.Tasks.Task` | Just don't introduce a non-generic class with the same name |
| SQL table names | Plural is fine (collection-of-rows convention) | `Utils.Operations` kept plural |
| `IServiceCollection` extension classes | Declared in `namespace Microsoft.Extensions.DependencyInjection` (with `#pragma warning disable IDE0130` + `// ReSharper disable once CheckNamespace`), so callers pick them up via the `using` they already have for `ServiceCollection` | [`MappingServiceCollectionExtensions`](src/mapping/Mpt.Framework.Mapping/MappingServiceCollectionExtensions.cs) |
| Extension method names on `IServiceCollection` | Informative but compact. Package-named umbrella for a single-flavour engine (`AddOperation`, `AddMessageHub`); flavour-prefixed when more than one flavour exists (`AddInMemoryMapping` vs `AddEfCoreMapping<TDbContext>`). Don't over-expand (`AddInMemoryEntityMappingServices` is too much). | `AddInMemoryMapping()` / `AddEfCoreMapping<TDbContext>()` |

## Standard dependency replacements

When you find these in upstream code, swap them for the OSS equivalents:

| Upstream | Replacement in OSS |
|---|---|
| `Mpt.Framework.Application.Serialization.GlobalSerializerOptions` | Local internal `<Component>SerializerOptions` (mirror the Configure method, drop converters that don't apply) |
| `Mpt.Framework.Application.Telemetry.TelemetryHelper` / telemetry filters | **Drop** entirely |
| `Mpt.Framework.Infrastructure.Configuration.FrameworkConfiguration` entry point | `IServiceCollection.Add<Component>(moduleCode, …)` extension method |
| `Mpt.Framework.Providers.EfCore.Configuration.IsEnum<>()` | Inline `HasConversion + HasMaxLength + IsRequired` in the entity config |
| `Mpt.Framework.Providers.EfCore.DbContextOptionsBuilderExtensions.AutoIncrement<>()` | `SaveChanges` override on the DbContext that bumps the version |
| `Mpt.Framework.Providers.EfCore.FrameworkDbContextProvider` (and its observers) | **Drop**; document the required `AddXEntity()` call in the README instead |
| `Mpt.Framework.Application.Events.*` (PlatformEvent, IPlatformEventEmitter, GenericEvent, etc.) | **Drop entirely** — that's the platform-events authoring layer, business-coupled to PlatformEntity. Users emit raw payloads (e.g. `EventMessage`) themselves. |
| `IPlatformMessageInspector` / hook interfaces | Simplify to an `Action<T>?` delegate on the builder (e.g. `OnMessagePublishing`) |
| `MassTransit.Serialization.JsonConverters.StringDecimalJsonConverter` removal hack | Just don't add it in the first place — we control the serializer options |
| `Mpt.Framework.Core.Models.IPlatformObject` / `IPlatformEntity` / `IRevisable` | **Don't redefine.** ProjectReference the existing `src/abstractions/Mpt.Framework.Abstractions` package and use the OSS interfaces as-is. The OSS package re-homes them to the root `Mpt.Framework` namespace (not upstream's `Mpt.Framework.Core.Models`), so the only port-time change is a `using` swap. Keep the distinction between `IPlatformObject` (id-only) and `IPlatformEntity` (id + revision) — several components rely on it (e.g. the `Mapping` engine treats them differently in its reference-vs-collection paths). Note: the OSS `IPlatformEntity` drops upstream's `Name` / `Icon` (display concerns); add them as plain properties on consuming entities when needed. |
| Upstream `src/Mpt.Framework.Application/Projections/` and `src/Mpt.Framework.Infrastructure/Projections/` (folder name) | OSS folder is `src/mapping/Mpt.Framework.Mapping{,.Abstractions}/`, package id `Mpt.Framework.Mapping`, namespace `Mpt.Framework.Mapping`. The OSS port covers only the dynamic-mapper half of upstream `Projections/`; the `IProjectionService<T>` cache wrapper was deliberately left behind. When cherry-picking later upstream changes, watch for path drift: an upstream commit touching `Projections/MappingExecutor.cs` lands in OSS `src/mapping/Mpt.Framework.Mapping/MappingExecutor.cs`. |
| `Mpt.Framework.Application.Exceptions.EntityNotFoundException` raised from an infrastructure helper | If raised inside a helper that can't reach a domain-level "not found" type, throw `KeyNotFoundException` with a descriptive message — the BCL primitive for "you asked me to look something up by key and it isn't there". Reserve a custom exception type only when callers need to catch it specifically. |

## What to skip from upstream

Per saved memories and prior session decisions:

- **`Mpt.Framework.Models.Shared`** — entire project is MPT business taxonomy. Skip everything including `Identifiers/`, `Accounts/`, `Helpdesk/`, `Visibility/`, `PlatformObject.cs`, `PlatformEntity.cs`, etc.
- **`Mpt.Framework.Core.Messaging.Access`** — `UserAccountType`, `EventMessagePrincipalAccess`, `EventMessageActor*`. Client/Vendor/Operations taxonomy is business semantics.
- **`Mpt.Framework.Core.Messaging.Request`** — API/Worker request context POCOs.
- **`Mpt.Framework.Application.Events.*`** and most of **`Mpt.Framework.Infrastructure.Events.*`** — platform-events authoring layer that is coupled to `IPlatformEntity` and `PlatformEntityMap`. Keep only thin transport pieces (e.g. `MessageHubPublisher.cs`, transport header constants).
- **EF Core Sync provider** (`Mpt.Framework.Providers.EfCore.Sync.*`) — depends on `SyncDataConsumer` + `PlatformEntityMap`.
- **Background publish-mode infrastructure** (`IPlatformMessagePublisher`, `BackgroundPlatformMessagePublisher`, `PlatformEventChannelService`, `PlatformEventBackgroundService`) — depends on `TracedTransport`, AppInsights, and the events authoring layer. Ship immediate publishing only; users can layer their own background queue.

When in doubt, follow the rule: **if it references `IPlatformEntity`, `UserAccountType`, `PlatformEntityMap`, or anything in `Models.Shared`, skip it or rewrite it generically.**

## Workflow — migrating a new component

### 1. Survey the upstream code

In the source clone:

```
git ls-tree -r --name-only HEAD | grep -i <component>
```

For each interesting file: `git show HEAD:<path>`. Note the dependencies (using statements, internal type references).

### 2. Decide scope

Walk the decision tree above. Identify:
- Files that go into Abstractions (clean POCOs/interfaces, no third-party deps).
- Files that go into the engine package (composition root concerns, MassTransit/EFCore wiring).
- Files that get an add-on package (EFCore, validation, etc.).
- Files to **skip** (business-coupled — see "What to skip").

### 3. Scaffold projects

Mirror an existing component's layout. Quickest path: copy the csproj files from an analogous component (operation is the fullest example) and substitute names.

Folder pattern:
```
src/<component>/Mpt.Framework.<Component>/Mpt.Framework.<Component>.csproj
src/<component>/Mpt.Framework.<Component>.Abstractions/Mpt.Framework.<Component>.Abstractions.csproj
src/<component>/Mpt.Framework.<Component>.EFCore/Mpt.Framework.<Component>.EFCore.csproj   (if needed)
tests/<component>/Mpt.Framework.<Component>.Tests/Mpt.Framework.<Component>.Tests.csproj
```

csproj checklist (mirror `Mpt.Framework.Operation/Mpt.Framework.Operation.csproj`):

- `<TargetFramework>net10.0</TargetFramework>`
- `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>`
- `<RootNamespace>` + `<AssemblyName>` set to package id
- `<IsPackable>true</IsPackable>` + `<PackageId>` + `<Title>`
- `<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>`
- `<Authors>SoftwareOne and the Mpt.Framework contributors</Authors>`
- `<Copyright>Copyright 2026 SoftwareOne and the Mpt.Framework contributors</Copyright>`
- `<PackageProjectUrl>https://github.com/SoftwareONE/mpt-framework-net</PackageProjectUrl>`
- `<RepositoryUrl>https://github.com/SoftwareONE/mpt-framework-net</RepositoryUrl>`
- `<IncludeSymbols>true</IncludeSymbols>` + `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + `<NoWarn>$(NoWarn);1591</NoWarn>`
- `<None Include="README.md" Pack="true" PackagePath="" Condition="Exists('README.md')" />`
- `<InternalsVisibleTo Include="Mpt.Framework.<Component>.Tests" />` on the engine project (and on the add-on if it consumes internals)

### 4. Copy & adapt source files

For each file decided to migrate:

- Update the namespace to `Mpt.Framework.<Component>[.*]`.
- Apply the standard dependency replacements (see table above).
- If a class has the same name as a sub-namespace it would live in, decide: namespace–type clash is OK if no shadow, otherwise rename internal types out of the way (see `OperationRegistration<,,>` precedent).

### 5. Migrate tests

Tests live in `tests/<component>/Mpt.Framework.<Component>.Tests/`.

- Use **xunit 2.9** + **FluentAssertions 6.12** + optionally **NSubstitute** (precedent matches existing test csprojs).
- `GlobalUsings.cs` with `global using Xunit;`.
- Organize: top-level for type-specific tests, `Configuration/` for builder/DI tests, `Functionality/` for end-to-end tests.
- End-to-end: use the InMemory transport, follow `OperationContext<T>` / `InMemoryRoundTripTests` patterns. Helper classes (sinks, test operations) should be PUBLIC nested when MassTransit's Castle DynamicProxy needs to see them.

### 6. Update the solution

Add to `Mpt.Framework.slnx`:
```xml
<Folder Name="/src/<component>/">
  <Project Path="src/<component>/Mpt.Framework.<Component>.Abstractions/Mpt.Framework.<Component>.Abstractions.csproj" />
  <Project Path="src/<component>/Mpt.Framework.<Component>/Mpt.Framework.<Component>.csproj" />
  <!-- + EFCore project if applicable -->
</Folder>
<Folder Name="/tests/<component>/">
  <Project Path="tests/<component>/Mpt.Framework.<Component>.Tests/Mpt.Framework.<Component>.Tests.csproj" />
</Folder>
```

### 7. Write the READMEs

One README per package. Mirror an existing one for tone and structure (`src/operation/Mpt.Framework.Operation/README.md` is the fullest reference). Include:

- One-paragraph description of what it does.
- Install instruction.
- Pointer to the Abstractions package (if applicable) for Application-layer users.
- Quick code example: define + register + use.
- Note about persistence options (if applicable).
- License line.

### 8. Verify

From this repo's root:

```
dotnet build Mpt.Framework.slnx
dotnet test Mpt.Framework.slnx --nologo --logger "console;verbosity=minimal"
```

- 0 errors required.
- New warnings should be 0 (pre-existing `Delta.Validation.Tests` nullability warnings are acceptable — they were there before).
- All existing tests still green.

### 9. Tidy and extend tests (review pass)

After the migration builds and the upstream tests pass, do a review pass. The Operation and MessageHub sessions established the pattern:

- Fix typos that crept in from upstream (e.g. `_succeded` → `_succeeded`, `_opearationId` → `_operationId`, `ShoulFailOnStart` → `ShouldFailOnStart`).
- Strip vacuous assertions (`Should().BeGreaterThanOrEqualTo(0)` proves nothing — replace with a meaningful invariant).
- Remove stale comments / dead `Task.Delay(N)` warmups.
- Cover failure paths that upstream tests miss — failure-mode enum values are excellent test targets (one test per `XFailureType.*` value).
- Cover builder validation paths (duplicate registration, invalid name, type-constraint violations, DI mode differences).
- New tests have caught real upstream bugs before (see `OperationStateArray(0)` issue → `NoTasks` dead-code path).

---

## Syncing later changes from the source repo

When upstream gets a new feature or bug fix you want to bring across:

### 1. Diff what's new

In the source clone:

```
git log <last-known-good-ref>..HEAD -- <component-path>
```

If you don't track a sync point, do a per-file diff:
```
git show HEAD:src/<component>/<file>.cs   # upstream version
```
…and compare to the OSS version at `src/<component>/Mpt.Framework.<Component>/<file>.cs`.

### 2. Apply changes with the same substitutions

Don't pull upstream files blind. Re-apply the same dependency replacements (table above) and skip rules (`Models.Shared`, business taxonomy, platform events).

### 3. Watch for new dependencies

A common trap: a new feature in upstream adds a `using Mpt.Framework.Application.<X>` that pulls in business code or new framework-internal deps. Decide whether to:
- **Adapt** — replace with a local equivalent.
- **Generalize** — turn a business-specific type into a delegate or `Dictionary<string, object>`.
- **Skip** — if the feature is fundamentally tied to MPT business semantics, leave it upstream.

### 4. Bring tests with the fix

If upstream added tests for the change, port them too — same translation rules.

### 5. Verify the same way

Build + test the whole solution. Watch for new warnings.

---

## Pitfalls

- **Castle DynamicProxy + private nested types**: MassTransit's `AddConsumer` etc. internally proxies generic interfaces parameterized by the consumer type. Strong-named test assemblies can't expose private nested types to `DynamicProxyGenAssembly2`. Use **public nested** test consumer / event classes inside `[Fact]` tests that go through MT registration.
- **`OperationStateArray(0)` style bugs**: when a guard at a later line in a method assumes an earlier line didn't throw, but the earlier line throws on zero/empty input → the guard becomes dead code. Worth a targeted test per failure-mode enum value.
- **In-memory filtering mismatch with Service Bus**: at least one upstream helper (`StreamRoutingHelper.ConditionSatisfied`) compares `TargetModules` to the wrong identifier in the in-memory path vs the Service Bus SQL-rule path. The annotation `[ExcludeFromCodeCoverage(Justification = "Test purposes only")]` suggests this was intentional/approximate. Don't add tests that pin down behavior the upstream code explicitly disclaims.
- **`OperationsBuilder` vs `OperationBuilder<,,>`** style conflicts when applying the singular rule: rename the internal generic out of the way **first**, then rename the public plural to singular. Otherwise a half-finished rename leaves a build break.
- **Dynamic saga type cache leakage in tests**: `OperationSagaTypeBuilder.MakeSagaType` caches by `Type`. Two tests using the same nested type with different `name` arguments will see the first test's name baked into both. Give each test its own private nested type.
- **`CollectionUpdateHelper.ProcessPrimitiveCollection` non-incrementing index** (Mapping): upstream loop reads `target[index]` against each source item but never advances `index`. Always comparing element 0 against every source item happens to work for "all equal" / "fully replaced" inputs but mis-detects same-prefix divergence. Fix it (`index++` at end of loop body) and add a targeted test — primitive collection in / primitive collection out where the change is in the middle.
- **`MapPathAsync` is "map up to and through this property", not "map only this property"** (Mapping): the path filter exits the loop *after* processing the matching entry, but entries iterated earlier are still processed. Path filtering chains across recursion (one queue head per level), so the right mental model is "stop after walking through the named property's subtree". Don't write tests that assume earlier entries are skipped — match upstream semantics or fix upstream first.
- **`protected internal` abstract members across assemblies must be overridden as plain `protected`** (Mapping.EFCore vs Mapping engine): if the abstract engine class exposes its extension points as `protected internal` (so `MappingExecutor` can call them via internal access while subclasses can override), a subclass in a *different* assembly cannot use `protected internal` on its override — the C# compiler emits CS0507 ("cannot change access modifiers"). Use plain `protected override` in the add-on's subclass; the virtual dispatch from the base class's `internal` call site still resolves to it.
- **EF Core InMemory provider doesn't model "navigation not loaded" semantics**: tests that try to assert collection navigations are empty before LoadAsync will fail because the InMemory provider eagerly fixes them up through reference tracking. Either accept the looseness and assert only on observable post-Map state, or move to SQLite-in-memory if you need real lazy/explicit-loading semantics.

## References

Existing migrations live in this repo as worked examples — copy their shape when in doubt:

- `src/abstractions/Mpt.Framework.Abstractions/` — the cross-cutting abstractions package. Hosts `IPlatformObject`, `IPlatformEntity`, `IRevisable` (re-homed from upstream's `Mpt.Framework.Core.Models` to the root `Mpt.Framework` namespace). When porting code that uses any of these, `ProjectReference` this package rather than redefining the interfaces locally; swap `using Mpt.Framework.Core.Models;` for `using Mpt.Framework;`.
- `src/delta/` — single-package component, no Abstractions split.
- `src/operation/` — Abstractions + main + EFCore split, fullest example.
- `src/messagehub/` — Abstractions + main, no EFCore.
- `src/mapping/` — Abstractions + main + EFCore. Best reference for migrations that depend on `Mpt.Rql` (sits on top of `IRqlMapAccessor` / `IRqlMapperContext`) and for migrations that lean on `IPlatformObject` / `IPlatformEntity` from `Mpt.Framework.Abstractions`. Worth a glance for the precedent of carving a tightly-scoped subset out of a much larger upstream namespace: upstream `Mpt.Framework.Application.Projections` and `Mpt.Framework.Infrastructure.Projections` cover both the dynamic mapper and an `IProjectionService<T>` cache wrapper coupled to the RQL Query subsystem; the OSS port intentionally ships only the mapper. Also a precedent for **renaming the OSS folder away from the upstream name**: upstream `Projections/` → OSS `mapping/` (and package `Mpt.Framework.Mapping`) because the cache wrapper that justified the "projection" wording isn't in scope. Also documents the precedent of dropping `UpdatablePropertyMapper` / `ComplexMappingContext` (the fluent custom-factory layer) while keeping `IUpdatableMappingFactory` so consumers can still wire factories via raw Mpt.Rql calls. The EFCore add-on (`Mpt.Framework.Mapping.EFCore`) is the cleanest precedent for an **EFCore add-on that subclasses an abstract engine type** (`DynamicEntityMapper`) and is registered with a generic `services.AddEfCoreMapping<TDbContext>()` extension that bridges the consumer's DbContext into the mapper's constructor — sidesteps the upstream `FrameworkDbContextProvider` and supports multi-DbContext setups for free.

The conventions in this skill are the canonical reference; they were originally captured as personal notes during the early migrations but are duplicated here so any contributor can apply them without privileged context.

## Keeping this skill current

When you finish migrating a new component, before signing off:

1. **Bump the count** in the TL;DR's opening sentence (`Three components` → `Four components` → …) and in the References heading.
2. **Add a bullet to the References list** with the folder path and a one-line note on what makes that migration a useful reference — what split it uses, what unusual dependency it depends on, what new dependency-substitution or skip-rule it established. This is the part future migrations will skim for "have we already solved something like this?", so make it specific.
3. **Promote new substitutions or skip rules** into the relevant tables (`Standard dependency replacements`, `What to skip from upstream`). If the migration only made a one-off decision, leave it as a note in the References bullet; if it generalises (any future migration touching upstream type X should do Y), the table is the right home.
4. **Add new pitfalls** to the `Pitfalls` section when the migration uncovered a non-obvious failure mode that wasn't already documented.
5. **Update saved memories** when the migration introduces a new cross-cutting convention (singular naming, `Abstractions` carve-out heuristic, etc.). Existing memory entries in `~/.claude/projects/.../memory/MEMORY.md` are the authoritative list; prefer updating an existing memory to adding a near-duplicate.

These steps make the skill self-updating in the practical sense: every successful migration extends the body of precedent it draws on, instead of leaving the next migrator to re-derive the same decisions.
