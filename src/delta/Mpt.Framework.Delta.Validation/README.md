# Mpt.Framework.Delta.Validation

FluentValidation integration for [`Mpt.Framework.Delta`](https://www.nuget.org/packages/Mpt.Framework.Delta). Provides a validator base class that knows how to walk a `Delta<T>` and produce error paths matching the incoming JSON structure (`"reference.name"`, `"collection[0].id"`, etc.).

The core `Mpt.Framework.Delta` package has **zero third-party runtime dependencies** — pull in this package only if you want the FluentValidation integration.

## Install

```
dotnet add package Mpt.Framework.Delta.Validation
```

Depends on **FluentValidation 11.x** (floating; the consumer's version wins within `[11.0.0, 12.0.0)`).

## Usage

```csharp
using FluentValidation;
using Mpt.Framework.Delta;
using Mpt.Framework.Delta.Validation;

public class UpdateUserValidator : DeltaValidator<UpdateUser>
{
    public UpdateUserValidator()
    {
        RuleForDelta(u => u.Name)
            .WhenDefined(t => t.NotEmpty().WithMessage("Name cannot be empty"));

        RuleForDelta(u => u.Email)
            .MustBeDefined(t => t.EmailAddress());

        RuleForDelta(u => u.Address)
            .WhenDefined()!
            .SetValidator(new AddressValidator());

        RuleForDelta(u => u.Tags)
            .WhenDefined()!
            .ForEachDelta(v => v.RuleForDelta(t => t.Name).MustBeDefined());
    }
}
```

Run it like any FluentValidation validator:

```csharp
var delta  = DeltaBuilder.FromJson<UpdateUser>(json);
var result = new UpdateUserValidator().Validate(delta);

if (!result.IsValid)
{
    foreach (var err in result.Errors)
    {
        // err.PropertyName follows the JSON path, e.g. "address.city", "tags[2].name"
    }
}
```

## Rule extensions

| Extension       | Meaning                                                          |
| --------------- | ---------------------------------------------------------------- |
| `MustBeDefined` | Property must be present in the payload (value may be null)      |
| `MustBeOmitted` | Property must not be present in the payload                      |
| `WhenDefined`   | Apply an inline value rule only if the property was present      |
| `ForEachDelta`  | Apply a child validator to every item in a collection delta      |

### Note on `WhenDefined()!.SetValidator(inner)`

The no-arg `WhenDefined()` is used to keep the fluent chain alive when attaching an
external sub-validator via `SetValidator(...)`. Be aware that the chained `SetValidator`
itself is **not** gated on the parent being defined — the sub-validator always runs
against the child delta. Sub-validators should therefore handle the "parent undefined"
case via their own `MustBeDefined` / `WhenDefined` rules (which is the natural pattern
anyway, since a missing parent makes every child delta undefined too).

If you really need to skip the sub-validator entirely when the parent is absent, gate
it with FluentValidation's native `.When(...)`:

```csharp
RuleForDelta(u => u.Address)
    .SetValidator(new AddressValidator())
    .When(d => d.IsDefined);
```

## License

Apache License 2.0 — see the [LICENSE](https://github.com/SoftwareONE/mpt-framework-net/blob/main/LICENSE) file at the repository root.
