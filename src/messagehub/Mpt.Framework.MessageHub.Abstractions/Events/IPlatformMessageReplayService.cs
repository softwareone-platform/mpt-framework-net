namespace Mpt.Framework.MessageHub;

/// <summary>
/// Re-drives an existing <see cref="EventMessage"/> back through the publish pipeline,
/// optionally with a delay and capped attempt count.
/// </summary>
public interface IPlatformMessageReplayService
{
    Task<bool> ReplayAsync(EventMessage message, string module, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, cancellationToken);

    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, CancellationToken cancellationToken)
        => ReplayAsync(message, module, stream, static p => { }, cancellationToken);

    Task<bool> ReplayAsync(EventMessage message, string module, Action<RetryPolicy> configure, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, configure, cancellationToken);

    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, Action<RetryPolicy> configure, CancellationToken cancellationToken)
    {
        var policy = new RetryPolicy();
        configure(policy);
        return ReplayAsync(message, module, stream, policy, cancellationToken);
    }

    Task<bool> ReplayAsync(EventMessage message, string module, RetryPolicy policy, CancellationToken cancellationToken)
        => ReplayAsync(message, module, message.Routing.Stream, policy, cancellationToken);

    /// <summary>
    /// Returns <see langword="false"/> when <see cref="EventMessage.Replays"/> already
    /// reached <see cref="RetryPolicy.MaxAttempts"/>; otherwise publishes a copy with
    /// the policy's delay and the routing redirected at <paramref name="module"/>.
    /// </summary>
    Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, RetryPolicy policy, CancellationToken cancellationToken);
}
