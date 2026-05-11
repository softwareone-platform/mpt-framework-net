using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaExtensionsTests
{
    [Fact]
    public void Split_OnDefinedArray_YieldsItemDeltasWithIndexedPaths()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""
            {"tags":[{"name":"a"},{"name":"b"},{"name":"c"}]}
            """);

        delta.TryGetDelta(u => u.Tags, out var tagsDelta).Should().BeTrue();
        var items = tagsDelta!.Split().ToList();

        items.Should().HaveCount(3);
        items[0].Data!.Name.Should().Be("a");
        items[0].Path.Should().Be("tags[0]");
        items[1].Path.Should().Be("tags[1]");
        items[2].Path.Should().Be("tags[2]");
    }

    [Fact]
    public void Split_OnEmptyArray_YieldsNothing()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"tags":[]}""");

        delta.TryGetDelta(u => u.Tags, out var tagsDelta).Should().BeTrue();
        tagsDelta!.Split().Should().BeEmpty();
    }

    [Fact]
    public void Split_OnAbsentCollection_YieldsNothing()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("{}");

        delta.TryGetDelta(u => u.Tags, out var tagsDelta);
        tagsDelta!.Split().Should().BeEmpty();
    }

    [Fact]
    public void Split_ItemDelta_AllowsChildPathChaining()
    {
        // After splitting, the returned item deltas should themselves be usable
        // as the basis for nested path resolution.
        var delta = DeltaBuilder.FromJson<TestUser>("""{"tags":[{"name":"hello"}]}""");

        delta.TryGetDelta(u => u.Tags, out var tagsDelta);
        var first = tagsDelta!.Split().Single();

        var nameDelta = first.GetDelta(t => t.Name);

        nameDelta.IsDefined.Should().BeTrue();
        nameDelta.Path.Should().Be("tags[0].name");
        nameDelta.Data.Should().Be("hello");
    }
}
