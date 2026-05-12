namespace Mpt.Framework.Mapping.Tests;

public class CollectionUpdateHelperTests
{
    [Fact]
    public void ProcessPrimitiveCollection_WhenAllItemsMatch_ReturnsZero()
    {
        var target = new List<object> { "a", "b", "c" };
        var source = new object[] { "a", "b", "c" };

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(0);
        target.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ProcessPrimitiveCollection_WhenSourceHasDifferentMiddleItem_ReplacesContent()
    {
        var target = new List<object> { "a", "b", "c" };
        var source = new object[] { "a", "x", "c" };

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(1);
        target.Should().Equal("a", "x", "c");
    }

    [Fact]
    public void ProcessPrimitiveCollection_WhenSourceLongerThanTarget_AppendsRest()
    {
        var target = new List<object> { "a" };
        var source = new object[] { "a", "b" };

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(1);
        target.Should().Equal("a", "b");
    }

    [Fact]
    public void ProcessPrimitiveCollection_WhenSourceShorterThanTarget_TrimsRest()
    {
        var target = new List<object> { "a", "b", "c" };
        var source = new object[] { "a", "b" };

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(1);
        target.Should().Equal("a", "b");
    }

    [Fact]
    public void ProcessPrimitiveCollection_WhenBothEmpty_ReturnsZero()
    {
        var target = new List<object>();
        var source = Array.Empty<object>();

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(0);
        target.Should().BeEmpty();
    }

    [Fact]
    public void ProcessPrimitiveCollection_WhenSourceEmptyAndTargetNot_ClearsTarget()
    {
        var target = new List<object> { "a" };
        var source = Array.Empty<object>();

        var updateCount = CollectionUpdateHelper.ProcessPrimitiveCollection(target, source);

        updateCount.Should().Be(1);
        target.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessUpdatableCollection_WithPlatformObjectItems_AddsMissingAndRemovesUnreferenced()
    {
        var existing1 = new PlatformObjectItem { Id = "1", Value = "one" };
        var existing2 = new PlatformObjectItem { Id = "2", Value = "two" };
        var target = new List<PlatformObjectItem> { existing1, existing2 };

        var source = new object[]
        {
            new PlatformObjectItem { Id = "1", Value = "one-updated" },
            new PlatformObjectItem { Id = "3", Value = "three" },
        };

        var (items, updateCount) = await CollectionUpdateHelper.ProcessUpdatableCollection(
            typeof(PlatformObjectItem),
            source,
            target);

        target.Should().HaveCount(2);
        target.Should().Contain(existing1);
        target.Should().NotContain(existing2);
        items.Should().HaveCount(2);
        items.Single(i => i.IsAdded).Source.Should().BeOfType<PlatformObjectItem>()
            .Which.Id.Should().Be("3");
        updateCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessUpdatableCollection_WithPlatformObjectItems_InvokesOnEntityRemoved()
    {
        var existing = new PlatformObjectItem { Id = "1", Value = "one" };
        var target = new List<PlatformObjectItem> { existing };
        var removed = new List<object>();

        await CollectionUpdateHelper.ProcessUpdatableCollection(
            typeof(PlatformObjectItem),
            Array.Empty<object>(),
            target,
            onEntityRemoved: entity =>
            {
                removed.Add(entity);
                return Task.CompletedTask;
            });

        removed.Should().ContainSingle(r => ReferenceEquals(r, existing));
    }

    [Fact]
    public async Task ProcessUpdatableCollection_WithNonPlatformObjectItems_ClearsAndReadds()
    {
        var target = new List<PlainItem> { new() { Value = "old" } };
        var source = new object[] { new PlainItem { Value = "new" }, new PlainItem { Value = "another" } };

        var (items, updateCount) = await CollectionUpdateHelper.ProcessUpdatableCollection(
            typeof(PlainItem),
            source,
            target);

        // Non-platform-object items are cleared then re-added as fresh instances. Property
        // values are populated later by the MappingExecutor walking each item; the helper
        // itself only manages the collection structure.
        target.Should().HaveCount(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(i => i.IsAdded);
        updateCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessAssignableCollection_WhenSourceContainsNewItem_AddsItViaLookup()
    {
        var existing = new NamedPlatformEntity { Id = "1", Name = "one" };
        var target = new List<NamedPlatformEntity> { existing };
        var byId = new Dictionary<string, NamedPlatformEntity>
        {
            ["1"] = existing,
            ["2"] = new NamedPlatformEntity { Id = "2", Name = "two" },
        };

        var source = new IPlatformEntity[]
        {
            new NamedPlatformEntity { Id = "1", Name = "ignored" },
            new NamedPlatformEntity { Id = "2", Name = "ignored" },
        };

        var updateCount = await CollectionUpdateHelper.ProcessAssignableCollection(
            typeof(NamedPlatformEntity),
            source,
            target,
            (_, item) => Task.FromResult<object?>(byId[item.Id]));

        target.Should().HaveCount(2);
        target.Select(t => t.Id).Should().BeEquivalentTo(["1", "2"]);
        updateCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAssignableCollection_WhenLookupReturnsNull_Throws()
    {
        var target = new List<NamedPlatformEntity>();
        var source = new IPlatformEntity[] { new NamedPlatformEntity { Id = "missing" } };

        var act = async () => await CollectionUpdateHelper.ProcessAssignableCollection(
            typeof(NamedPlatformEntity),
            source,
            target,
            (_, _) => Task.FromResult<object?>(null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    public class PlatformObjectItem : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class NamedPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string? Name { get; init; }
        public string? Icon { get; init; }
    }

    public class PlainItem
    {
        public string Value { get; set; } = string.Empty;
    }
}
