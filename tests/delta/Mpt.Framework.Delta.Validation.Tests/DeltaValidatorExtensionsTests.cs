using FluentValidation;
using Mpt.Framework.Delta.Validation.Tests.Utility;

namespace Mpt.Framework.Delta.Validation.Tests;

public class DeltaValidatorExtensionsTests
{
    // ----- MustBeDefined -----

    [Fact]
    public void MustBeDefined_WhenPropertyAbsent_ReportsRequiredError()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeDefined());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("{}"));

        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(
                e => e.PropertyName == "name" && e.ErrorMessage == "Property must be provided");
    }

    [Fact]
    public void MustBeDefined_WhenPropertyPresent_HasNoError()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeDefined());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}"""));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MustBeDefined_WhenPropertyPresentAsNull_StillSatisfiesPresenceCheck()
    {
        // Explicit null counts as "defined". If you also need non-null, combine with .NotNull()
        // via the value-rule overload.
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeDefined());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":null}"""));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MustBeDefined_WithValueRule_RunsValueRuleWhenDefined()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name)
            .MustBeDefined(t => t.NotEmpty().WithMessage("must not be empty")));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":""}"""));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("must not be empty");
    }

    [Fact]
    public void MustBeDefined_WithValueRule_SkipsValueRuleWhenAbsent_ReportsOnlyPresenceError()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name)
            .MustBeDefined(t => t.NotEmpty().WithMessage("must not be empty")));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("{}"));

        // Only the presence error fires — the value rule needs a value to evaluate.
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Property must be provided");
    }

    // ----- MustBeOmitted -----

    [Fact]
    public void MustBeOmitted_WhenPropertyAbsent_HasNoError()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeOmitted());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("{}"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MustBeOmitted_WhenPropertyPresent_ReportsForbiddenError()
    {
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeOmitted());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}"""));

        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(
                e => e.PropertyName == "name" && e.ErrorMessage == "Property must be omitted");
    }

    [Fact]
    public void MustBeOmitted_WhenPropertyPresentAsNull_ReportsForbiddenError()
    {
        // null still counts as "sent" — so MustBeOmitted should reject it.
        var validator = new InlineValidator(v => v.RuleForDelta(u => u.Name).MustBeOmitted());

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":null}"""));

        result.Errors.Should().ContainSingle();
    }

    // ----- WhenDefined -----

    [Fact]
    public void WhenDefined_WhenPropertyAbsent_SkipsInnerRule()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).WhenDefined(t => t.NotEmpty().WithMessage("inner")));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("{}"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WhenDefined_WhenPropertyPresentAndInnerFails_ReportsInnerError()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).WhenDefined(t => t.NotEmpty().WithMessage("inner")));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":""}"""));

        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(
                e => e.PropertyName == "name" && e.ErrorMessage == "inner");
    }

    [Fact]
    public void WhenDefined_WhenPropertyPresentAndInnerPasses_HasNoError()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Name).WhenDefined(t => t.NotEmpty().WithMessage("inner")));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}"""));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WhenDefined_NoArguments_KeepsChainAliveForExternalValidator()
    {
        // The no-arg overload exists so callers can chain SetValidator(...) onto an
        // external sub-validator. The sub-validator receives the child delta as-is;
        // it is responsible for handling the "parent undefined" case via its own
        // MustBeDefined / WhenDefined rules. (The chained SetValidator is NOT itself
        // gated on IsDefined — that behavior would require .When(d => d.IsDefined)
        // applied directly after SetValidator.)
        var inner = new InlineAddressValidator(a => a.RuleForDelta(x => x.City).MustBeDefined());
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Address).WhenDefined()!.SetValidator(inner));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""{"address":{"street":"Main"}}"""));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("address.city");
    }

    // ----- ForEachDelta -----

    [Fact]
    public void ForEachDelta_WithSharedValidator_AppliesToEachItem()
    {
        var itemValidator = new InlineTagValidator(t => t.RuleForDelta(x => x.Name).MustBeDefined());
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Tags).WhenDefined()!.ForEachDelta(itemValidator));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""
            {"tags":[{"name":"ok"},{},{"name":"also-ok"}]}
            """));

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("tags[1].name");
    }

    [Fact]
    public void ForEachDelta_WithInlineConfiguration_AppliesToEachItem()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Tags)
                .WhenDefined()!
                .ForEachDelta(c => c.RuleForDelta(t => t.Name).MustBeDefined()));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""
            {"tags":[{},{},{"name":"ok"}]}
            """));

        result.Errors.Should().HaveCount(2);
        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(["tags[0].name", "tags[1].name"]);
    }

    // ----- Path -----

    [Fact]
    public void Path_NavigatesToNestedPropertyAndAppliesRule()
    {
        var validator = new InlineValidator(v =>
            v.RuleForDelta(u => u.Address)
                .Path(a => a.City, c => c.MustBeDefined()));

        var result = validator.Validate(DeltaBuilder.FromJson<TestUser>("""
            {"address":{"street":"Main"}}
            """));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("address.city");
    }

    // ----- helpers -----

    private sealed class InlineValidator : DeltaValidator<TestUser>
    {
        public InlineValidator(Action<InlineValidator> configure) => configure(this);
    }

    private sealed class InlineAddressValidator : DeltaValidator<TestAddress>
    {
        public InlineAddressValidator(Action<InlineAddressValidator> configure) => configure(this);
    }

    private sealed class InlineTagValidator : DeltaValidator<TestTag>
    {
        public InlineTagValidator(Action<InlineTagValidator> configure) => configure(this);
    }
}
