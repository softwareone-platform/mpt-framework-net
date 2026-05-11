using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.Configuration;

[ExcludeFromCodeCoverage(Justification = "Configuration")]
public class ProcessingOptions
{
    public GroupProcessingOptions Main { get; } = new()
    {
        Concurrency = 5,
        MaxAttempts = 5,
        MinProcessingTime = TimeSpan.FromMinutes(3),
        MaxProcessingTime = TimeSpan.FromMinutes(5)
    };

    public GroupProcessingOptions Tasks { get; } = new()
    {
        Concurrency = 5,
        MaxAttempts = 5,
        MinProcessingTime = TimeSpan.FromMinutes(3),
        MaxProcessingTime = TimeSpan.FromMinutes(10)
    };
}

[ExcludeFromCodeCoverage(Justification = "Configuration")]
public class GroupProcessingOptions
{
    public int Concurrency { get; set; } = 5;

    public int PrefetchCount => (int)(Concurrency * 1.5);

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan MinProcessingTime { get; set; } = TimeSpan.FromMinutes(3);

    public TimeSpan MaxProcessingTime { get; set; } = TimeSpan.FromMinutes(10);

    public ExponentialRetryOptions? Retry { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Configuration")]
public class ExponentialRetryOptions
{
    public int RetryLimit { get; set; } = 6;

    public TimeSpan MinInterval { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaxInterval { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan IntervalDelta { get; set; } = TimeSpan.FromSeconds(12);

    public TimeSpan ProcessingTime { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan CalculateMaxDelay()
    {
        double totalSeconds = 0;

        for (int attempt = 1; attempt <= RetryLimit; attempt++)
        {
            // Exponential delay, capped by maxInterval
            double delaySeconds = MinInterval.TotalSeconds + IntervalDelta.TotalSeconds * Math.Pow(2, attempt - 1);
            delaySeconds = Math.Min(delaySeconds, MaxInterval.TotalSeconds);

            // Add delay + estimated processing time
            totalSeconds += delaySeconds + ProcessingTime.TotalSeconds;
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }

    public Func<Exception, bool> Filter { get; set; } = static _ => false;
}
