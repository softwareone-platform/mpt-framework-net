namespace Mpt.Framework.MessageHub;

/// <summary>
/// Controls how <see cref="IPlatformMessageReplayService"/> re-drives a failed
/// <see cref="EventMessage"/>: the upper bound on attempts and how the delay grows.
/// </summary>
public class RetryPolicy
{
    /// <summary>Maximum number of replay attempts before the service gives up.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Growth pattern for the delay between attempts.</summary>
    public RetryMode Mode { get; set; } = RetryMode.Linear;

    /// <summary>Initial delay applied to the first replay attempt.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Computes the delay for the given <paramref name="attempt"/> (1-based).</summary>
    public TimeSpan GetDelay(int attempt) => Mode switch
    {
        RetryMode.Fixed => InitialDelay,
        RetryMode.Linear => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * attempt),
        RetryMode.Exponential => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
        _ => throw new NotImplementedException($"The retry mode '{Mode}' is not implemented.")
    };
}

/// <summary>Delay-growth pattern for <see cref="RetryPolicy"/>.</summary>
public enum RetryMode
{
    /// <summary>Same delay on every attempt.</summary>
    Fixed,
    /// <summary>Delay scales linearly with the attempt number.</summary>
    Linear,
    /// <summary>Delay doubles with each attempt.</summary>
    Exponential
}
