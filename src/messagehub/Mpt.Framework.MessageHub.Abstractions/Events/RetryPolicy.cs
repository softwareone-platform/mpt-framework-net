namespace Mpt.Framework.MessageHub;

public class RetryPolicy
{
    public int MaxAttempts { get; set; } = 3;

    public RetryMode Mode { get; set; } = RetryMode.Linear;

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(3);

    public TimeSpan GetDelay(int attempt) => Mode switch
    {
        RetryMode.Fixed => InitialDelay,
        RetryMode.Linear => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * attempt),
        RetryMode.Exponential => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
        _ => throw new NotImplementedException($"The retry mode '{Mode}' is not implemented.")
    };
}

public enum RetryMode
{
    Fixed,
    Linear,
    Exponential
}
