using FluentAssertions;

namespace Mpt.Framework.Persistence.Tests;

public class InMemoryQueryExecutionStrategyTests
{
    private readonly InMemoryQueryExecutionStrategy _strategy = InMemoryQueryExecutionStrategy.Instance;

    [Fact]
    public async Task CountAsync_ReturnsQueryableCount()
    {
        var data = new[] { 1, 2, 3, 4, 5 }.AsQueryable();

        var count = await _strategy.CountAsync(data, CancellationToken.None);

        count.Should().Be(5);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatchingItem()
    {
        var data = new[] { 10, 20, 30 }.AsQueryable();

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
        var data = new[] { "a", "b", "c" }.AsQueryable();

        var list = await _strategy.ToListAsync(data, CancellationToken.None);

        list.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public void Instance_IsSingletonAcrossLookups()
    {
        InMemoryQueryExecutionStrategy.Instance.Should().BeSameAs(InMemoryQueryExecutionStrategy.Instance);
    }
}
