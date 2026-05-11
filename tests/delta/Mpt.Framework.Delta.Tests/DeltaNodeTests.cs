using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaNodeTests
{
    [Fact]
    public void Copy_PreservesShapeAcrossNestedNodeTypes()
    {
        // Single test covers the three node kinds (object, array, value) plus nesting.
        var delta = DeltaBuilder.FromJson<TestUser>("""
            {
              "name": "Alice",
              "address": { "city": "NYC" },
              "tags": [ { "name": "a" }, { "name": "b" } ]
            }
            """);

        var copy = delta.Node!.Copy();

        copy.Should().BeOfType<DeltaObjectNode>();
        copy.TryGetChild("name", out var nameNode).Should().BeTrue();
        nameNode.Should().BeOfType<DeltaValueNode>();

        copy.TryGetChild("address", out var addressNode).Should().BeTrue();
        addressNode.Should().BeOfType<DeltaObjectNode>();
        addressNode!.TryGetChild("city", out var cityNode).Should().BeTrue();
        cityNode.Should().BeOfType<DeltaValueNode>();

        copy.TryGetChild("tags", out var tagsNode).Should().BeTrue();
        tagsNode.Should().BeOfType<DeltaArrayNode>();
        tagsNode!.Children.Should().HaveCount(2);
        tagsNode.Children.Should().AllBeOfType<DeltaObjectNode>();
    }

    [Fact]
    public void Copy_DoesNotCarryDataValues()
    {
        // Copy duplicates structure for MapTo to attach NEW typed data; original data
        // is intentionally not transferred to the new node tree.
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        var copy = (DeltaObjectNode)delta.Node!.Copy();

        copy.TryGetChild("name", out var nameNode).Should().BeTrue();
        nameNode!.Data.Should().BeNull();
    }

    [Fact]
    public void TryGetChild_IsCaseInsensitive()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.Node!.TryGetChild("NAME", out var node).Should().BeTrue();
        node.Should().NotBeNull();
    }

    [Fact]
    public void TryGetChild_OnMissingKey_ReturnsFalse()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.Node!.TryGetChild("does-not-exist", out var node).Should().BeFalse();
        node.Should().BeNull();
    }

    [Fact]
    public void Indexer_ReturnsChildrenInRegistrationOrder()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""
            {"tags":[{"name":"first"},{"name":"second"},{"name":"third"}]}
            """);

        delta.Node!.TryGetChild("tags", out var tagsNode);
        var arrayNode = (DeltaArrayNode)tagsNode!;

        arrayNode[0].Name.Should().Be("item_0");
        arrayNode[1].Name.Should().Be("item_1");
        arrayNode[2].Name.Should().Be("item_2");
    }

    [Fact]
    public void Children_EnumeratesAllRegisteredChildren()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""
            {"name":"Alice","address":{"city":"NYC"}}
            """);

        var names = delta.Node!.Children.Select(c => c.Name).ToList();

        names.Should().BeEquivalentTo(["name", "address"]);
    }
}
