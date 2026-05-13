using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class RetryPolicyTests
{
    [Fact]
    public void Defaults_AreLinearWith3AttemptsAndThreeSecondInitialDelay()
    {
        var policy = new RetryPolicy();

        policy.MaxAttempts.Should().Be(3);
        policy.Mode.Should().Be(RetryMode.Linear);
        policy.InitialDelay.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void GetDelay_Fixed_AlwaysReturnsTheInitialDelay()
    {
        var policy = new RetryPolicy { Mode = RetryMode.Fixed, InitialDelay = TimeSpan.FromSeconds(2) };

        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(5).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(100).Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GetDelay_Linear_ScalesByAttempt()
    {
        var policy = new RetryPolicy { Mode = RetryMode.Linear, InitialDelay = TimeSpan.FromSeconds(2) };

        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(2).Should().Be(TimeSpan.FromSeconds(4));
        policy.GetDelay(3).Should().Be(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void GetDelay_Exponential_DoublesEachAttempt()
    {
        var policy = new RetryPolicy { Mode = RetryMode.Exponential, InitialDelay = TimeSpan.FromSeconds(1) };

        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(1));
        policy.GetDelay(2).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(3).Should().Be(TimeSpan.FromSeconds(4));
        policy.GetDelay(4).Should().Be(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void GetDelay_OnUnknownMode_ThrowsNotImplemented()
    {
        var policy = new RetryPolicy { Mode = (RetryMode)999 };

        var act = () => policy.GetDelay(1);

        act.Should().Throw<NotImplementedException>().WithMessage("*999*");
    }
}
