#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System.Collections.Generic;

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<List<T>> BatchAsync<T>(this IAsyncEnumerable<T> source, int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var batch = new List<T>(batchSize);

        await foreach (var item in source)
        {
            batch.Add(item);

            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize); // Start a new batch
            }
        }

        if (batch.Count > 0)
        {
            yield return batch; // Yield remaining items
        }
    }
}
