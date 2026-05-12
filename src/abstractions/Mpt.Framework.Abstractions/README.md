# Mpt.Framework.Abstractions

Foundational abstractions shared across the `Mpt.Framework.*` package family. Zero third-party dependencies.

## What's inside

| Type                                 | Purpose                                                                                          |
| ------------------------------------ | ------------------------------------------------------------------------------------------------ |
| `Mpt.Framework.IPlatformObject`      | Anything that carries a stable string `Id`. Components use this to match instances by identity.  |
| `Mpt.Framework.IRevisable`           | Anything that carries a monotonically-increasing `Revision`. Used for optimistic concurrency.    |
| `Mpt.Framework.IPlatformEntity`      | A revisable identifiable — the composition of `IPlatformObject` and `IRevisable`.                |

## When to install

Install this package directly whenever your code needs to implement, accept, or detect one of the three interfaces above. Most consumers will pick it up transitively through another framework package.

```
dotnet add package Mpt.Framework.Abstractions
```

Target framework: **net10.0**.

## Example

```csharp
using Mpt.Framework;

public sealed class Invoice : IPlatformEntity
{
    public string Id { get; set; } = string.Empty;
    public int Revision { get; set; }
}
```

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
