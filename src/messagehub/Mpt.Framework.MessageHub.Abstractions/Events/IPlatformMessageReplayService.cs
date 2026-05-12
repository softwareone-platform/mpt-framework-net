namespace Mpt.Framework.MessageHub;

/// <summary>
/// Re-drives an existing <see cref="EventMessage"/> back through the publish pipeline,
/// optionally with a delay and capped attempt count. Resolved as a scoped service from
/// <c>AddMessageHub</c>; consumers opt into retry by calling <see cref="ReplayAsync(EventMessage, string, CancellationToken)"/>.
/// </summary>
public interface IPlatformMessageReplayService
{
    /// <summary>Replay using the original stream and a default <see cref="RetryPolicy"/>.</summary>
    Task<bool> ReplayAsync(EventMessage message, string module, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, cancellationToken);

    /// <summary>Replay onto a specific stream with a default <see cref="RetryPolicy"/>.</summary>
    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, CancellationToken cancellationToken)
        => ReplayAsync(message, module, stream, static p => { }, cancellationToken);

    /// <summary>Replay using the original stream with an inline-configured <see cref="RetryPolicy"/>.</summary>
    Task<bool> ReplayAsync(EventMessage message, string module, Action<RetryPolicy> configure, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, configure, cancellationToken);

    /// <summary>Replay onto a specific stream with an inline-configured <see cref="RetryPolicy"/>.</summary>
    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, Action<RetryPolicy> configure, CancellationToken cancellationToken)
    {
        var policy = new RetryPolicy();
        configure(policy);
        return ReplayAsync(message, module, stream, policy, cancellationToken);
    }

    /// <summary>Replay using the original stream and an explicit <see cref="RetryPolicy"/>.</summary>
    Task<bool> ReplayAsync(EventMessage message, string module, RetryPolicy policy, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, policy, cancellationToken);

    /// <summary>
    /// Replays the message onto <paramref name="stream"/> targeting <paramref name="module"/>,
    /// applying <paramref name="policy"/>'s delay and attempt cap. Returns <see langword="false"/>
    /// when the attempt count would exceed <see cref="RetryPolicy.MaxAttempts"/>.
    /// </summary>
    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, RetryPolicy policy, CancellationToken cancellationToken);
}
