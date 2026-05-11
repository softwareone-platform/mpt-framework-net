# Mpt.Framework.Operations.EntityFrameworkCore

SQL Server persistence add-on for [`Mpt.Framework.Operations`](https://www.nuget.org/packages/Mpt.Framework.Operations). The main package ships with in-memory persistence; install this companion to keep saga state across process restarts.

## Install

```
dotnet add package Mpt.Framework.Operations.EntityFrameworkCore
```

Depends on **EF Core 10.x** and **MassTransit.EntityFrameworkCore 8.x**.

## Setup

### 1. Register persistence

```csharp
using Mpt.Framework.Operations;

services.AddOperations("billing", ops =>
{
    ops.Settings.Mode = OperationsMode.ConsumeAndDispatch;
    ops.Settings.Transport = OperationsTransport.ServiceBus;
    ops.Settings.ConnectionString = configuration.ServiceBus.ConnectionString;

    ops.UseSqlServerPersistence(configuration.Sql.ConnectionString);

    ops.Register<InvoiceBatchOperation>("invoice.batch");
});
```

`UseSqlServerPersistence` registers an internal `OperationsDbContext` that owns the saga table, plus the EF Core saga repository configuration.

### 2. Add the operations entity to your primary DbContext

```csharp
using Microsoft.EntityFrameworkCore;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddOperationsEntity();
    base.OnModelCreating(modelBuilder);
}
```

This adds the `Utils.Operations` table to your model so that EF Core migrations can create / track it alongside your own entities. A discriminator column distinguishes saga rows belonging to different operation types.

### 3. Create a migration

```
dotnet ef migrations add AddOperationsEntity
dotnet ef database update
```

## How it works

- Sagas are stored as one row per running operation in `Utils.Operations`.
- The `Version` column is incremented on every save and acts as a concurrency token (snapshot isolation).
- The operation contract is serialized to a `JsonObject` column so the finished handler can rehydrate it.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
