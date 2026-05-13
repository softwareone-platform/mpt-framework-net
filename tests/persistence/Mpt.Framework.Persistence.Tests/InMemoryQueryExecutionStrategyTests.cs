using FluentAssertions;

namespace Mpt.Framework.Persistence.Tests;

public class InMemoryQueryExecutionStrategyTests
{
    private static readonly int[] _fiveIntegers = [1, 2, 3, 4, 5];
    private static readonly int[] _threeIntegers = [10, 20, 30];
    private static readonly string[] _threeStrings = ["a", "b", "c"];

    private readonly InMemoryQueryExecutionStrategy _strategy = InMemoryQueryExecutionStrategy.Instance;

    [Fact]
    public async Task CountAsync_ReturnsQueryableCount()
    {
        var data = _fiveIntegers.AsQueryable();

        var count = await _strategy.CountAsync(data, CancellationToken.None);

        count.Should().Be(5);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatchingItem()
    {
        var data = _threeIntegers.AsQueryable();

        var first = await _strategy.FirstOrDefaultAsync(data, CancellationToken.None);

        first.Should().Be(10);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_OnEmptyQueryable_ReturnsDefault()
    {
        var data = Array.Empty<string>().AsQueryable();

        var first = await _strategy.FirstOrDefaultAsync(data, CancellationToken.None);

        first.Should().BeNull();
    }

    [Fact]
    public async Task ToListAsync_MaterializesAllItems()
    {
        var data = _threeStrings.AsQueryable();

        var list = await _strategy.ToListAsync(data, CancellationToken.None);

        list.Should().BeEquivalentTo(_threeStrings);
    }

    [Fact]
    public void Instance_IsSingletonAcrossLookups()
    {
        InMemoryQueryExecutionStrategy.Instance.Should().BeSameAs(InMemoryQueryExecutionStrategy.Instance);
    }
}
