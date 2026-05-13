using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class InputStreamSettingsTests
{
    [Fact]
    public void Defaults_MatchDocumentedValues()
    {
        var settings = new InputStreamSettings();

        settings.MaxDeliveryCount.Should().Be(3);
        settings.PrefetchCount.Should().Be(16);
        settings.ConcurrentMessagesLimit.Should().Be(8);
        settings.AutoDeleteOnIdle.Should().Be(TimeSpan.FromDays(60));
        settings.DefaultMessageTimeToLive.Should().Be(TimeSpan.FromDays(7));
        settings.LockDuration.Should().Be(TimeSpan.FromMinutes(1));
        settings.MaxAutoRenewDuration.Should().BeNull();
        settings.RequiresSession.Should().BeFalse();
        settings.MaxConcurrentCallsPerSession.Should().BeNull();
        settings.MaxConcurrentSessions.Should().BeNull();
        settings.SessionIdleTimeout.Should().BeNull();
        settings.ImmediateMessageRetryLimit.Should().BeNull();
    }

    [Fact]
    public void Setters_StoreSuppliedValues()
    {
        var settings = new InputStreamSettings
        {
            MaxDeliveryCount = 7,
            PrefetchCount = 32,
            ConcurrentMessagesLimit = 4,
            AutoDeleteOnIdle = TimeSpan.FromDays(1),
            DefaultMessageTimeToLive = TimeSpan.FromHours(12),
            LockDuration = TimeSpan.FromSeconds(45),
            MaxAutoRenewDuration = TimeSpan.FromMinutes(10),
            RequiresSession = true,
            MaxConcurrentCallsPerSession = 2,
            MaxConcurrentSessions = 5,
            SessionIdleTimeout = TimeSpan.FromMinutes(3),
            ImmediateMessageRetryLimit = 5,
        };

        settings.MaxDeliveryCount.Should().Be(7);
        settings.PrefetchCount.Should().Be(32);
        settings.ConcurrentMessagesLimit.Should().Be(4);
        settings.AutoDeleteOnIdle.Should().Be(TimeSpan.FromDays(1));
        settings.DefaultMessageTimeToLive.Should().Be(TimeSpan.FromHours(12));
        settings.LockDuration.Should().Be(TimeSpan.FromSeconds(45));
        settings.MaxAutoRenewDuration.Should().Be(TimeSpan.FromMinutes(10));
        settings.RequiresSession.Should().BeTrue();
        settings.MaxConcurrentCallsPerSession.Should().Be(2);
        settings.MaxConcurrentSessions.Should().Be(5);
        settings.SessionIdleTimeout.Should().Be(TimeSpan.FromMinutes(3));
        settings.ImmediateMessageRetryLimit.Should().Be(5);
    }
}
