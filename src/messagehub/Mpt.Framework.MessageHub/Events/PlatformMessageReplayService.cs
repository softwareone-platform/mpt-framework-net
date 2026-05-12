using System.Diagnostics;

namespace Mpt.Framework.MessageHub;

internal class PlatformMessageReplayService(IPlatformMessagePublisher messagePublisher) : IPlatformMessageReplayService
{
    public async Task<bool> ReplayAsync(EventMessage message, string module, StreamTypes stream, RetryPolicy policy, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(module);

        if (message.Replays >= policy.MaxAttempts)
        {
            return false;
        }

        message.Replays++;
        message.Routing.Delay = policy.GetDelay(message.Replays);
        message.Routing.Stream = stream;
        message.Routing.TargetModules = [module];

        await messagePublisher.PublishAsync(new(message, Activity.Current?.Context), cancellationToken);
        return true;
    }
}
