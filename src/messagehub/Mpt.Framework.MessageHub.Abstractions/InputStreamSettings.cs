namespace Mpt.Framework.MessageHub;

public class InputStreamSettings
{
    public int MaxDeliveryCount { get; set; } = 3;

    public int PrefetchCount { get; set; } = 16;

    public int ConcurrentMessagesLimit { get; set; } = 8;

    public TimeSpan AutoDeleteOnIdle { get; set; } = TimeSpan.FromDays(60);

    public TimeSpan DefaultMessageTimeToLive { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Lock duration for the message on Azure Service Bus. Default 1 minute, max 5 minutes.
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum duration for automatic lock renewal. MassTransit will keep renewing the
    /// message lock until this duration is reached.
    /// </summary>
    public TimeSpan? MaxAutoRenewDuration { get; set; }

    /// <summary>
    /// When true, requires inbound messages to specify a session id. Publishers set the
    /// session id from <see cref="EventMessage.SessionId"/>.
    /// </summary>
    public bool RequiresSession { get; set; }

    /// <summary>
    /// Maximum number of concurrent calls per session. Only applicable when
    /// <see cref="RequiresSession"/> is true.
    /// </summary>
    public int? MaxConcurrentCallsPerSession { get; set; }

    /// <summary>
    /// Maximum number of concurrent sessions. Only applicable when
    /// <see cref="RequiresSession"/> is true.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Idle timeout for a session before it is closed. Only applicable when
    /// <see cref="RequiresSession"/> is true.
    /// </summary>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>
    /// When set, creates an immediate retry policy with the specified number of retries
    /// (no delay between attempts).
    /// </summary>
    public int? ImmediateMessageRetryLimit { get; set; }
}
