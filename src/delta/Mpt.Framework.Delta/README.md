# Mpt.Framework.Delta

Strongly-typed JSON partial update (delta) model for .NET. Lets your API distinguish between three states of an incoming property:

1. **Omitted** — the client did not send the property
2. **Explicitly null** — the client sent `"prop": null`
3. **Set to a value** — the client sent `"prop": "foo"`

Standard `JsonSerializer` collapses cases (1) and (2) into a single `null`, which makes it impossible to implement HTTP PATCH semantics correctly. `Delta<T>` preserves which fields the client actually touched, so your update code can apply only those.

## Install

```
dotnet add package Mpt.Framework.Delta
```

Target framework: **net10.0**. The core package has **zero third-party runtime dependencies**.

For FluentValidation integration, add the companion package [`Mpt.Framework.Delta.Validation`](https://www.nuget.org/packages/Mpt.Framework.Delta.Validation).

## Quick start

### Define a model

```csharp
public class UpdateUser
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Address? Address { get; set; }
}
```

### Build a delta from JSON

```csharp
using Mpt.Framework.Delta;

var json = """{ "name": "Alice", "email": null }""";
var delta = DeltaBuilder.FromJson<UpdateUser>(json);

delta.TryGet(u => u.Name,    out var name);     // true,  "Alice"
delta.TryGet(u => u.Email,   out var email);    // true,  null   (explicit null)
delta.TryGet(u => u.Address, out var address);  // false, default (omitted)
```

`TryGet` returns `true` when the property was present in the JSON — even if its value was `null`. Use that to drive the update:

```csharp
if (delta.TryGet(u => u.Name, out var name))
    user.Name = name;

if (delta.TryGet(u => u.Email, out var email))
    user.Email = email;       // explicitly cleared
```

Or use the convenience helper:

```csharp
delta.AssignIfDefined(u => u.Name,  v => user.Name = v);
delta.AssignIfDefined(u => u.Email, v => user.Email = v);
```

### Nested deltas

`GetDelta` / `TryGetDelta` give you a delta for a child node, preserving the same omitted/null/value distinction:

```csharp
if (delta.TryGetDelta(u => u.Address, out var addressDelta))
{
    addressDelta.AssignIfDefined(a => a.Street, v => user.Address.Street = v);
    addressDelta.AssignIfDefined(a => a.City,   v => user.Address.City   = v);
}
```

### Collections

`Split()` enumerates an array delta as a sequence of element deltas, each with its full JSON path:

```csharp
if (delta.TryGetDelta(u => u.Tags, out var tags))
{
    foreach (var tag in tags.Split())
    {
        // tag.Path is e.g. "tags[0]", "tags[1]", ...
    }
}
```

### Map to another type

`MapTo<TTarget>` serializes the delta's data through JSON and rebuilds the same shape on the target type — useful when an API contract and a domain entity have different shapes but share property names:

```csharp
Delta<UpdateUser>   incoming = DeltaBuilder.FromJson<UpdateUser>(json);
Delta<UserEntity>   mapped   = incoming.MapTo<UserEntity>();
```

## ASP.NET Core integration

Register the converter factory so `Delta<T>` parameters bind from request bodies:

```csharp
services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new DeltaJsonConverterFactory());
});

// Controller
[HttpPatch("{id}")]
public IActionResult Patch(int id, [FromBody] Delta<UpdateUser> patch) { ... }
```

The converter respects `[JsonPropertyName]` on your model and treats property names case-insensitively with the CamelCase naming policy.

## Validation

The validator integration lives in a separate optional NuGet package, [`Mpt.Framework.Delta.Validation`](https://www.nuget.org/packages/Mpt.Framework.Delta.Validation), to keep this core package free of third-party runtime dependencies. See that package for `DeltaValidator<T>`, `MustBeDefined`, `WhenDefined`, `ForEachDelta`, etc.

## How it works (briefly)

`DeltaBuilder` parses the incoming JSON into a tree of `DeltaNode`s (`DeltaObjectNode` / `DeltaArrayNode` / `DeltaValueNode`) that mirrors the shape of the payload — but only at the properties the client actually sent. The deserialized POCO is attached to the root node. When you call `GetDelta(u => u.X)`, the library walks both the lambda's member chain and the node tree in lockstep: a missing node means the property was omitted; a present node with `null` data means it was set to `null`.

`[JsonPropertyName]` is honored for member-name lookup; otherwise property names are matched with the CamelCase naming policy.

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
