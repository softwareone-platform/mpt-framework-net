using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests.Events;

public class EventObjectFactoryTests
{
    [Fact]
    public void Make_DefaultsToCurrentEntityCategoryAndTypeFromTypeName()
    {
        var entity = new TestEntity { Id = "E1" };

        var result = EventObjectFactory.Make(entity, "entity_key");

        result.Id.Should().Be("E1");
        result.Key.Should().Be("entity_key");
        result.Type.Should().Be(nameof(TestEntity));
        result.Category.Should().Be(EventMessageObjectCategory.CurrentEntity);
        result.Data.Should().BeSameAs(entity);
        result.Name.Should().BeNull();
        result.Icon.Should().BeNull();
    }

    [Fact]
    public void Make_WithIsOriginalTrue_ReturnsOriginalEntityCategory()
    {
        var entity = new TestEntity { Id = "E1" };

        var result = EventObjectFactory.Make(entity, "entity_key", isOriginalValue: true);

        result.Category.Should().Be(EventMessageObjectCategory.OriginalEntity);
    }

    [Fact]
    public void Make_WithExplicitCategory_UsesProvidedCategory()
    {
        var entity = new TestEntity { Id = "E1" };

        var result = EventObjectFactory.Make(entity, "entity_key", EventMessageObjectCategory.Custom);

        result.Category.Should().Be(EventMessageObjectCategory.Custom);
    }

    [Fact]
    public void MakeAdditional_ReturnsAdditionalEntityCategory()
    {
        var entity = new TestEntity { Id = "E1" };

        var result = EventObjectFactory.MakeAdditional(entity, "additional_key");

        result.Id.Should().Be("E1");
        result.Key.Should().Be("additional_key");
        result.Category.Should().Be(EventMessageObjectCategory.AdditionalEntity);
        result.Data.Should().BeSameAs(entity);
    }

    [Fact]
    public void MakeAdditionalCustom_WithArbitraryData_UsesGivenTypeAndDoesNotReadNameOrIcon()
    {
        var data = new { Foo = "bar" };

        var result = EventObjectFactory.MakeAdditionalCustom(data, "obj-1", "custom_key", "CustomType");

        result.Id.Should().Be("obj-1");
        result.Key.Should().Be("custom_key");
        result.Type.Should().Be("CustomType");
        result.Category.Should().Be(EventMessageObjectCategory.AdditionalEntity);
        result.Data.Should().BeSameAs(data);
        result.Name.Should().BeNull();
        result.Icon.Should().BeNull();
    }
}
