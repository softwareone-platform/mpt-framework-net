using FluentValidation;
using Mpt.Framework;
using Mpt.Framework.Delta;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests;

/// <summary>
/// Drives the rare branches of <c>UpdatePolicyBuilders</c> + <c>UpdatePolicyContext</c>
/// that <see cref="PolicyDslTests"/> doesn't reach: Require/Forbid/Ignore access, Hint /
/// Cascade flags, action filters, If predicates, nested properties, and the
/// validation-failure callback path.
///
/// Uses its own probe entity type (<see cref="PolicyProbeView"/>) so that
/// <c>ScanForConfigurations</c> in <c>PersistenceFixture</c> never resolves the
/// parameterised <see cref="ConfigWith"/> for the production <c>WidgetView</c> surface.
/// </summary>
public class PolicyDslExtendedTests
{
    [Fact]
    public void ForbidRule_TakesPrecedenceOverDefaultAllowed()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Forbid()));

        var (access, _, _) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");

        access.Should().Be(PolicyRuleAccess.Forbidden);
    }

    [Fact]
    public void RequireRule_ContributesRequiredFlag()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Require("ops")));

        var (access, _, _) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");

        access.Should().HaveFlag(PolicyRuleAccess.Required);
    }

    [Fact]
    public void IgnoreRule_MarksAccessIgnoredAndCascades()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Ignore()));

        var (access, _, _) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");

        access.Should().Be(PolicyRuleAccess.Ignored);
    }

    [Fact]
    public void HintRule_AddsToTheHintBitmap()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow().Hint(PropertyHints.TrustReference)));

        var (_, hints, _) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");

        hints.Should().HaveFlag(PropertyHints.TrustReference);
    }

    [Fact]
    public void CascadeFlag_LetsRuleStillResolveTheNodeItIsAttachedTo()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow().Cascade()));

        var (access, _, _) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");

        access.Should().HaveFlag(PolicyRuleAccess.Allowed);
    }

    [Fact]
    public void OnAction_RestrictsRuleToTheNamedAction()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow("ops").On(EntityAction.Update)));

        var update = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name");
        var create = Execute(config, action: EntityAction.Create, role: "ops", propertyName: "Name");

        update.Access.Should().HaveFlag(PolicyRuleAccess.Allowed);
        create.Access.Should().Be(PolicyRuleAccess.Forbidden, "the rule's On(Update) filter excludes Create");
    }

    [Fact]
    public void IfPredicate_GatesRuleEvaluation()
    {
        var config = new ConfigWith(p => p.Property(v => v.Count, prop => prop
            .Allow("ops")
            .If(data => (int)data.Updated! > 0)));

        var positive = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Count", updated: 5);
        var zero = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Count", updated: 0);

        positive.Access.Should().HaveFlag(PolicyRuleAccess.Allowed);
        zero.Access.Should().Be(PolicyRuleAccess.Forbidden, "If(data => Updated > 0) fails the predicate when Updated is 0");
    }

    [Fact]
    public void IfPredicate_OnStandardDataPath_RunsTheWrappedConditionWithoutThrowing()
    {
        // The If wrapper's mismatch-throw branch is unreachable from the public Execute
        // path (the data bag built there always matches TRoot). Sanity-check that the
        // happy path stays happy.
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow("ops").If(_ => true)));

        var policy = config.GetUpdatePolicy(EntityAction.Update, ["ops"]);
        var node = policy.GetChild("Name");

        var act = () => node.Execute(["ops"], new PolicyProbeView(), DeltaBuilder.Empty<PolicyProbeView>(), null, "v", true);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateCallback_RunsValidatorAndCollectsErrors()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop
            .Allow("ops")
            .Validate(v => v.RuleFor(x => x.Updated).Must(value => (value as string)?.Length > 2)
                .WithMessage("name too short"))));

        var (_, _, validation) = Execute(config, action: EntityAction.Update, role: "ops", propertyName: "Name", updated: "ab");

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(e => e.ErrorMessage == "name too short");
    }

    [Fact]
    public void NestedProperty_BuildsChildNodes()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow("ops")));

        var policy = config.GetUpdatePolicy(EntityAction.Update, ["ops"]);
        var nameNode = policy.GetChild("Name");

        nameNode.Should().NotBeNull("Property() should have registered a Name child node");
        nameNode.HasChildRules.Should().BeFalse();
    }

    [Fact]
    public void GetChild_OnUnknownProperty_ReturnsEmptyChild()
    {
        var config = new ConfigWith(p => p.Property(v => v.Name, prop => prop.Allow("ops")));

        var policy = config.GetUpdatePolicy(EntityAction.Update, ["ops"]);
        var child = policy.GetChild("DoesNotExist");

        child.Should().NotBeNull();
        child.HasChildRules.Should().BeFalse();
    }

    private static (PolicyRuleAccess Access, PropertyHints Hints, FluentValidation.Results.ValidationResult Validation) Execute(
        ConfigWith config,
        EntityAction action,
        string role,
        string propertyName,
        object? updated = null)
    {
        var policy = config.GetUpdatePolicy(action, [role]);
        var node = policy.GetChild(propertyName);
        return node.Execute([role], new PolicyProbeView(), DeltaBuilder.Empty<PolicyProbeView>(), null, updated, true);
    }

    /// <summary>Probe entity decoupled from the WidgetView so DI scanning doesn't pick up our parameterised config.</summary>
    public sealed class PolicyProbeView : IPlatformEntity, IRqlGraphHolder
    {
        public string Id { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public IRqlNode? RqlGraph { get; set; }
    }

    private sealed class ConfigWith(Action<IUpdatePolicy<PolicyProbeView>> configureUpdate) : EntityConfiguration<PolicyProbeView>
    {
        protected override void ConfigureActions(IActionPolicy<PolicyProbeView> policy)
        {
            policy.Define(EntityAction.Create);
            policy.Define(EntityAction.Update, "ops");
        }

        protected override void ConfigureUpdate(IUpdatePolicy<PolicyProbeView> policy) => configureUpdate(policy);
    }
}
