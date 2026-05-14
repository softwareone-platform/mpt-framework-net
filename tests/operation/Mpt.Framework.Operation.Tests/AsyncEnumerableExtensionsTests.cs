using FluentAssertions;

namespace Mpt.Framework.Operation.Tests;

public class AsyncEnumerableExtensionsTests
{
    [Fact]
    public async Task BatchAsync_GroupsItemsIntoFixedSizeBatches()
    {
        var batches = new List<List<int>>();

        await foreach (var batch in AsyncRange(1, 7).BatchAsync(3))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(3);
        batches[0].Should().Equal(1, 2, 3);
        batches[1].Should().Equal(4, 5, 6);
        batches[2].Should().Equal(7);
    }

    [Fact]
    public async Task BatchAsync_WithExactMultiple_YieldsNoTailBatch()
    {
        var batches = new List<List<int>>();

        await foreach (var batch in AsyncRange(1, 6).BatchAsync(2))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(3);
        batches.Should().AllSatisfy(b => b.Should().HaveCount(2));
    }

    [Fact]
    public async Task BatchAsync_WithEmptySource_YieldsNothing()
    {
        var batches = new List<List<int>>();

        await foreach (var batch in AsyncRange(0, 0).BatchAsync(5))
        {
            batches.Add(batch);
        }

        batches.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchAsync_WithZeroBatchSize_Throws()
    {
        var act = async () =>
        {
            await foreach (var _ in AsyncRange(1, 3).BatchAsync(0))
            {
                // Should not reach here.
            }
        };

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static async IAsyncEnumerable<int> AsyncRange(int start, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return start + i;
        }
    }
}
