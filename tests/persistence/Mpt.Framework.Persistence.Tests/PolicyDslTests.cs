using Mpt.Framework.Delta;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

public class PolicyDslTests
{
    [Fact]
    public void IsActionAllowed_with_matching_role_returns_true()
    {
        var config = new WidgetConfig();

        config.IsActionAllowed(EntityAction.Update, ["ops"]).Should().BeTrue();
        config.IsActionAllowed(EntityAction.Update, ["client"]).Should().BeFalse();
    }

    [Fact]
    public void IsActionAllowed_with_unrestricted_define_accepts_any_role()
    {
        var config = new WidgetConfig();

        // Create was defined without a role filter — open to any caller.
        config.IsActionAllowed(EntityAction.Create, ["anyone"]).Should().BeTrue();
        config.IsActionAllowed(EntityAction.Create, []).Should().BeTrue();
    }

    [Fact]
    public void IsActionAllowed_for_unknown_action_returns_false()
    {
        var config = new WidgetConfig();
        config.IsActionAllowed("Archive", ["ops"]).Should().BeFalse();
    }

    [Fact]
    public void GetUpdatePolicy_evaluates_property_rule_against_supplied_roles()
    {
        var config = new WidgetConfig();
        var policy = config.GetUpdatePolicy(EntityAction.Update, ["ops"]);

        var nameNode = policy.GetChild("Name");
        var (access, _, _) = nameNode.Execute(["ops"], new WidgetView(), DeltaBuilder.Empty<WidgetView>(), null, "new-name", true);

        access.Should().HaveFlag(PolicyRuleAccess.Allowed);
    }

    [Fact]
    public void GetUpdatePolicy_forbidden_role_resolves_to_Forbidden()
    {
        var config = new WidgetConfig();
        var policy = config.GetUpdatePolicy(EntityAction.Update, ["client"]);

        var nameNode = policy.GetChild("Name");
        // Name is only Allowed for ops; client gets the default (Forbidden).
        var (access, _, _) = nameNode.Execute(["client"], new WidgetView(), DeltaBuilder.Empty<WidgetView>(), null, "new-name", true);

        access.Should().Be(PolicyRuleAccess.Forbidden);
    }

    [Fact]
    public void GetUpdatePolicy_validator_reports_failure_when_predicate_fails()
    {
        var config = new WidgetConfig();
        var policy = config.GetUpdatePolicy(EntityAction.Update, ["ops"]);
        var countNode = policy.GetChild("Count");

        // Count.Validate requires Updated > 0; supply 0 and assert the validator failed.
        var (_, _, validation) = countNode.Execute(["ops"], new WidgetView(), DeltaBuilder.Empty<WidgetView>(), 5, 0, true);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("must be positive"));
    }

    private sealed class WidgetConfig : EntityConfiguration<WidgetView>
    {
        protected override void ConfigureActions(IActionPolicy<WidgetView> policy)
        {
            policy.Define(EntityAction.Create);                  // open to all roles
            policy.Define(EntityAction.Update, "ops");           // ops only
            policy.Define(EntityAction.Delete, "ops");
        }

        protected override void ConfigureUpdate(IUpdatePolicy<WidgetView> policy)
        {
            policy.Property(v => v.Name, p => p.Allow("ops"));
            policy.Property(v => v.Count, p => p.Allow("ops")
                .Validate(v => v.RuleFor(x => x.Updated)
                    .Must(c => c > 0).WithMessage("Count must be positive")));
        }
    }
}
