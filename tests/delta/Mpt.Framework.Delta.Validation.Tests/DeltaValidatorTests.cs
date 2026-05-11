using FluentValidation;
using Mpt.Framework.Delta.Validation.Tests.Utility;

namespace Mpt.Framework.Delta.Validation.Tests;

public class DeltaValidatorTests
{
    [Fact]
    public void Validate_OnTopLevelFailure_UsesJsonPathAsPropertyName()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).MustBeDefined());

        var delta = DeltaBuilder.FromJson<TestUser>("{}");
        var result = validator.Validate(delta);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("name");
    }

    [Fact]
    public void Validate_OnNestedFailure_BuildsCompoundJsonPath()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Address)
                .WhenDefined()!
                .SetValidator(new InlineAddressValidator(a =>
                    a.RuleForDelta(x => x.City).MustBeDefined())));

        var delta = DeltaBuilder.FromJson<TestUser>("""{"address":{"street":"Main"}}""");
        var result = validator.Validate(delta);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("address.city");
    }

    [Fact]
    public void Validate_OnIndexedItemFailure_BuildsIndexedPath()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Tags)
                .WhenDefined()!
                .ForEachDelta(c => c.RuleForDelta(t => t.Name).MustBeDefined()));

        var delta = DeltaBuilder.FromJson<TestUser>("""
            {"tags":[{"name":"ok"},{}]}
            """);
        var result = validator.Validate(delta);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("tags[1].name");
    }

    [Fact]
    public async Task ValidateAsync_AppliesSamePathAdjustmentAsValidate()
    {
        // ValidateAsync must adjust property names the same way Validate does — otherwise
        // callers that use the async pipeline would see internal-looking names like
        // "Validate.Item.Path".
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).MustBeDefined());

        var delta = DeltaBuilder.FromJson<TestUser>("{}");
        var result = await validator.ValidateAsync(delta);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("name");
    }

    [Fact]
    public void Validate_OnValidInput_HasNoErrors()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).MustBeDefined());

        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");
        var result = validator.Validate(delta);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_AcrossMultipleRules_ReportsAllFailuresWithCorrectPaths()
    {
        // Integration check: a realistic validator with several rule types produces
        // the full set of expected error paths in one pass.
        var validator = new InlineValidator(v =>
        {
            v.RuleForDelta(u => u.Name).MustBeDefined(t => t.NotEmpty().WithMessage("Name must not be empty"));
            v.RuleForDelta(u => u.Address)
                .WhenDefined()!
                .SetValidator(new InlineAddressValidator(a => a.RuleForDelta(x => x.City).MustBeDefined()));
            v.RuleForDelta(u => u.Tags)
                .WhenDefined()!
                .ForEachDelta(c => c.RuleForDelta(t => t.Name).MustBeDefined());
        });

        var delta = DeltaBuilder.FromJson<TestUser>("""
            {
              "name": "",
              "address": { "street": "Main" },
              "tags": [ { "name": "ok" }, { } ]
            }
            """);

        var result = validator.Validate(delta);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "name" && e.ErrorMessage == "Name must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == "address.city");
        result.Errors.Should().Contain(e => e.PropertyName == "tags[1].name");
    }

    // ------ helpers ------

    private sealed class InlineValidator : DeltaValidator<TestUser>
    {
        public InlineValidator(Action<InlineValidator> configure) => configure(this);
    }

    private sealed class InlineAddressValidator : DeltaValidator<TestAddress>
    {
        public InlineAddressValidator(Action<InlineAddressValidator> configure) => configure(this);
    }
}
