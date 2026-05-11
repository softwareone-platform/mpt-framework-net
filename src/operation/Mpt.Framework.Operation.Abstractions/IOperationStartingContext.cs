namespace Mpt.Framework.Operation;

public interface IOperationStartingContext<out TOperationData> : IOperationContext<TOperationData>
{
    /// <summary>
    /// Current attempt (1 based).
    /// </summary>
    int Attempt { get; set; }

    /// <summary>
    /// Postpone operation start for the specified delay.
    /// </summary>
    /// <param name="delay">Time to delay before next attempt</param>
    void Postpone(TimeSpan delay);
}
