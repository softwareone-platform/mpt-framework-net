using FluentAssertions;
using Mpt.Framework.Persistence.Internal;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

/// <summary>
/// Small targeted tests for the rare-branch lines of the in-memory policy collections —
/// duplicate registration throws, empty-roles fallthrough, and the unknown-action check.
/// </summary>
public class EventAndActionPolicyEdgeTests
{
    [Fact]
    public void EventPolicy_Define_OnDuplicateAction_Throws()
    {
        var policy = new EventPolicy<WidgetView>();
        policy.Define(EntityAction.Create);

        var act = () => policy.Define(EntityAction.Create);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Create*already*");
    }

    [Fact]
    public void EventPolicy_IsDefined_AnswersTrueForRegisteredAction()
    {
        var policy = new EventPolicy<WidgetView>();
        policy.Define(EntityAction.Update);

        policy.IsDefined(EntityAction.Update).Should().BeTrue();
        policy.IsDefined(EntityAction.Delete).Should().BeFalse();
    }

    [Fact]
    public void ActionPolicy_Define_OnDuplicateActionName_Throws()
    {
        var policy = new ActionPolicy<WidgetView>();
        policy.Define("Archive");

        var act = () => policy.Define("Archive");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Archive*already*");
    }

    [Fact]
    public void ActionPolicy_IsAllowed_ForUnknownAction_ReturnsFalse()
    {
        var policy = new ActionPolicy<WidgetView>();

        policy.IsAllowed("Archive", ["ops"]).Should().BeFalse();
    }

    [Fact]
    public void ActionPolicy_IsAllowed_WithNoRolesOnDefine_AcceptsAnyCaller()
    {
        var policy = new ActionPolicy<WidgetView>();
        policy.Define("Read");

        policy.IsAllowed("Read", ["client"]).Should().BeTrue();
        policy.IsAllowed("Read", []).Should().BeTrue();
    }
}
