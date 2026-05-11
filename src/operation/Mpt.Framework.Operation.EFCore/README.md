# Mpt.Framework.Operation.EFCore

SQL Server persistence add-on for [`Mpt.Framework.Operation`](https://www.nuget.org/packages/Mpt.Framework.Operation). The main package ships with in-memory persistence; install this companion to keep saga state across process restarts.

## Install

```
dotnet add package Mpt.Framework.Operation.EFCore
```

Depends on **EF Core 10.x** and **MassTransit.EntityFrameworkCore 8.x**.

## Setup

### 1. Register persistence

```csharp
using Mpt.Framework.Operation;

services.AddOperation("billing", ops =>
{
    ops.Settings.Mode = OperationMode.ConsumeAndDispatch;
    ops.Settings.Transport = OperationTransport.ServiceBus;
    ops.Settings.ConnectionString = configuration.ServiceBus.ConnectionString;

    ops.UseSqlServerPersistence(configuration.Sql.ConnectionString);

    ops.Register<InvoiceBatchOperation>("invoice.batch");
});
```

`UseSqlServerPersistence` registers an internal `OperationDbContext` that owns the saga table, plus the EF Core saga repository configuration.

### 2. Add the operation entity to your primary DbContext

```csharp
using Microsoft.EntityFrameworkCore;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddOperationEntity();
    base.OnModelCreating(modelBuilder);
}
```

This adds the `Utils.Operations` table to your model so that EF Core migrations can create / track it alongside your own entities. A discriminator column distinguishes saga rows belonging to different operation types.

### 3. Create a migration

```
dotnet ef migrations add AddOperationEntity
dotnet ef database update
```

## How it works

- Sagas are stored as one row per running operation in `Utils.Operations`.
- The `Version` column is incremented on every save and acts as a concurrency token (snapshot isolation).
- The operation contract is serialized to a `JsonObject` column so the finished handler can rehydrate it.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
