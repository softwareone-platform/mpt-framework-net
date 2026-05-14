using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class DynamicEntityMapperBehaviorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly TestDynamicEntityMapper _mapper;

    public DynamicEntityMapperBehaviorTests()
    {
        var services = new ServiceCollection();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(DynamicEntityMapperBehaviorTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
        _rqlMapAccessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        _mapper = new TestDynamicEntityMapper(_serviceProvider, _rqlMapAccessor);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MapAsync_WithNullComplexProperty_ShouldCreateNewInstance()
    {
        var source = new TestEntity { Id = "1", ComplexProperty = new TestComplexProperty { Value = "test" } };
        var target = new TestDbEntity { Id = "1" };

        await _mapper.MapAsync(source, target);

        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("test");
    }

    [Fact]
    public async Task MapAsync_WithExistingComplexProperty_ShouldReuseAndUpdateInstance()
    {
        var existingComplexProperty = new TestComplexProperty { Value = "old" };
        var source = new TestEntity { Id = "1", ComplexProperty = new TestComplexProperty { Value = "new" } };
        var target = new TestDbEntity { Id = "1", ComplexProperty = existingComplexProperty };

        await _mapper.MapAsync(source, target);

        target.ComplexProperty.Should().BeSameAs(existingComplexProperty);
        target.ComplexProperty!.Value.Should().Be("new");
    }

    [Fact]
    public async Task MapAsync_WithSimpleProperty_ShouldMapCorrectly()
    {
        var source = new TestEntity { Id = "1", Name = "Test Name" };
        var target = new TestDbEntity { Id = "1" };

        await _mapper.MapAsync(source, target);

        target.Name.Should().Be("Test Name");
    }

    [Fact]
    public async Task MapAsync_WithNestedProperty_ShouldMapCorrectly()
    {
        var source = new TestEntity
        {
            Id = "1",
            ComplexProperty = new TestComplexProperty { Value = "Nested Value" },
        };
        var target = new TestDbEntity { Id = "1" };

        await _mapper.MapAsync(source, target);

        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("Nested Value");
    }

    [Fact]
    public async Task MapAsync_WithMissingTargetProperty_ShouldContinueGracefully()
    {
        var source = new TestEntityWithExtraProperty { Id = "1", Name = "Test", ExtraProperty = "extra" };
        var target = new TestDbEntity { Id = "1" };

        var act = async () => await _mapper.MapAsync(source, target);
        await act.Should().NotThrowAsync();

        target.Name.Should().Be("Test");
    }

    [Fact]
    public async Task MapAsync_WithNullSourceCollection_ShouldSetTargetToNull()
    {
        var source = new TestEntityWithCollection { Id = "1", Items = null };
        var target = new TestDbEntityWithCollection
        {
            Id = "1",
            Items = ["existing"],
        };

        await _mapper.MapAsync(source, target);

        target.Items.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_WithNullTargetCollection_ShouldCreateNewCollection()
    {
        var source = new TestEntityWithCollection { Id = "1", Items = ["item1", "item2"] };
        var target = new TestDbEntityWithCollection { Id = "1", Items = null };

        await _mapper.MapAsync(source, target);

        target.Items.Should().NotBeNull();
        target.Items.Should().BeEquivalentTo(["item1", "item2"]);
    }

    [Fact]
    public async Task MapAsync_WithExistingCollection_ShouldReplaceItems()
    {
        var source = new TestEntityWithCollection { Id = "1", Items = ["new1", "new2"] };
        var target = new TestDbEntityWithCollection
        {
            Id = "1",
            Items = ["old1", "old2", "old3"],
        };

        await _mapper.MapAsync(source, target);

        target.Items.Should().BeEquivalentTo(["new1", "new2"]);
    }

    [Fact]
    public async Task MapAsync_WithComplexCollection_ShouldMapNestedObjects()
    {
        var source = new TestEntityWithComplexCollection
        {
            Id = "1",
            ComplexItems =
            [
                new TestComplexItem { Id = "1", Value = "Value1" },
                new TestComplexItem { Id = "2", Value = "Value2" },
            ],
        };
        var target = new TestDbEntityWithComplexCollection { Id = "1", ComplexItems = [] };

        await _mapper.MapAsync(source, target);

        target.ComplexItems.Should().HaveCount(2);
        target.ComplexItems.Should().Contain(x => x.Id == "1" && x.Value == "Value1");
        target.ComplexItems.Should().Contain(x => x.Id == "2" && x.Value == "Value2");
    }

    [Fact]
    public async Task MapAsync_WithCollectionLoadingDisabled_ShouldSkipUpdate()
    {
        var source = new TestEntityWithCollection { Id = "1", Items = ["new1", "new2"] };
        var target = new TestDbEntityWithCollection
        {
            Id = "1",
            Items = ["existing"],
        };

        _mapper.EnsureCollectionLoadedAsyncResult = false;

        await _mapper.MapAsync(source, target);

        target.Items.Should().Equal("existing");
    }

    [Fact]
    public async Task MapAsync_WithNullSource_ShouldHandleGracefully()
    {
        var target = new TestDbEntity { Id = "1" };

        var act = async () => await _mapper.MapAsync<TestDbEntity, TestEntity>(null!, target);

        try
        {
            await act.Invoke();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<StackOverflowException>();
        }
    }

    [Fact]
    public async Task MapAsync_WithNullTarget_ShouldHandleGracefully()
    {
        var source = new TestEntity { Id = "1", Name = "Test" };

        var act = async () => await _mapper.MapAsync<TestDbEntity, TestEntity>(source, null!);

        try
        {
            await act.Invoke();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeOfType<StackOverflowException>();
        }
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntity_ShouldCallUpdatePlatformEntityReference()
    {
        var platformEntity = new TestPlatformEntity { Id = "platform-1" };
        var source = new TestEntityWithPlatformProperty
        {
            Id = "1",
            PlatformEntity = platformEntity,
        };
        var target = new TestDbEntityWithPlatformProperty { Id = "1" };

        _mapper.UseAssignForPlatformEntitiesOverride = true;

        await _mapper.MapAsync(source, target);

        _mapper.UpdatePlatformEntityReferenceCalls.Should().HaveCount(1);
        _mapper.UpdatePlatformEntityReferenceCalls[0].entity.Should().Be(platformEntity);
    }

    [Fact]
    public void MapModes_EnumValues_ShouldHaveCorrectFlags()
    {
        DynamicEntityMapper.MapModes.None.Should().Be((DynamicEntityMapper.MapModes)0);
        DynamicEntityMapper.MapModes.Primitive.Should().Be((DynamicEntityMapper.MapModes)1);
        DynamicEntityMapper.MapModes.Complex.Should().Be((DynamicEntityMapper.MapModes)2);
        DynamicEntityMapper.MapModes.All.Should().Be(DynamicEntityMapper.MapModes.Primitive | DynamicEntityMapper.MapModes.Complex);
    }

    [Fact]
    public void MapModes_HasFlag_ShouldWorkCorrectly()
    {
        var allMode = DynamicEntityMapper.MapModes.All;
        var primitiveMode = DynamicEntityMapper.MapModes.Primitive;
        var complexMode = DynamicEntityMapper.MapModes.Complex;

        allMode.HasFlag(DynamicEntityMapper.MapModes.Primitive).Should().BeTrue();
        allMode.HasFlag(DynamicEntityMapper.MapModes.Complex).Should().BeTrue();
        primitiveMode.HasFlag(DynamicEntityMapper.MapModes.Complex).Should().BeFalse();
        complexMode.HasFlag(DynamicEntityMapper.MapModes.Primitive).Should().BeFalse();
    }

    [Fact]
    public async Task MapPrimitiveAsync_ShouldOnlyMapPrimitiveProperties()
    {
        var source = new TestEntity
        {
            Id = "1",
            Name = "Test",
            ComplexProperty = new TestComplexProperty { Value = "Complex" },
        };
        var target = new TestDbEntity { Id = "1", Name = "Old" };

        await _mapper.MapPrimitiveAsync(source, target);

        target.Name.Should().Be("Test");
        target.ComplexProperty.Should().BeNull();
    }

    [Fact]
    public async Task MapComplexAsync_ShouldOnlyMapComplexProperties()
    {
        var source = new TestEntity
        {
            Id = "1",
            Name = "Test",
            ComplexProperty = new TestComplexProperty { Value = "Complex" },
        };
        var target = new TestDbEntity { Id = "1", Name = "Old" };

        await _mapper.MapComplexAsync(source, target);

        target.Name.Should().Be("Old");
        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("Complex");
    }

    public class TestEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public TestComplexProperty? ComplexProperty { get; set; }
    }

    public class TestDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public TestComplexProperty? ComplexProperty { get; set; }
    }

    public class TestComplexProperty
    {
        public string Value { get; set; } = null!;
    }

    public class TestEntityWithCollection
    {
        public string Id { get; set; } = null!;
        public List<string>? Items { get; set; }
    }

    public class TestDbEntityWithCollection
    {
        public string Id { get; set; } = null!;
        public List<string>? Items { get; set; }
    }

    public class TestComplexItem
    {
        public string Id { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class TestEntityWithComplexCollection
    {
        public string Id { get; set; } = null!;
        public List<TestComplexItem> ComplexItems { get; set; } = [];
    }

    public class TestDbEntityWithComplexCollection
    {
        public string Id { get; set; } = null!;
        public List<TestComplexItem> ComplexItems { get; set; } = [];
    }

    public class TestEntityWithExtraProperty
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string ExtraProperty { get; set; } = null!;
    }

    public class TestPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = null!;
        public int Revision { get; set; }
    }

    public class TestEntityWithPlatformProperty
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? PlatformEntity { get; set; }
    }

    public class TestDbEntityWithPlatformProperty
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? PlatformEntity { get; set; }
    }

    public class TestEntityToDbMap : IRqlMapper<TestDbEntity, TestEntity>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntity, TestEntity> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Name, d => d.Name);
            context.MapDynamic(v => v.ComplexProperty, d => d.ComplexProperty);
        }
    }

    public class TestComplexPropertyMap : IRqlMapper<TestComplexProperty, TestComplexProperty>
    {
        public void MapEntity(IRqlMapperContext<TestComplexProperty, TestComplexProperty> context)
        {
            context.MapDynamic(v => v.Value, d => d.Value);
        }
    }

    public class TestEntityWithExtraPropertyMap : IRqlMapper<TestDbEntity, TestEntityWithExtraProperty>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntity, TestEntityWithExtraProperty> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Name, d => d.Name);
        }
    }

    public class TestEntityWithCollectionMap : IRqlMapper<TestDbEntityWithCollection, TestEntityWithCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithCollection, TestEntityWithCollection> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Items, d => d.Items);
        }
    }

    public class TestComplexItemMap : IRqlMapper<TestComplexItem, TestComplexItem>
    {
        public void MapEntity(IRqlMapperContext<TestComplexItem, TestComplexItem> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Value, d => d.Value);
        }
    }

    public class TestEntityWithComplexCollectionMap : IRqlMapper<TestDbEntityWithComplexCollection, TestEntityWithComplexCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithComplexCollection, TestEntityWithComplexCollection> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.ComplexItems, d => d.ComplexItems);
        }
    }

    public class TestEntityWithPlatformPropertyMap : IRqlMapper<TestDbEntityWithPlatformProperty, TestEntityWithPlatformProperty>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPlatformProperty, TestEntityWithPlatformProperty> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.PlatformEntity, d => d.PlatformEntity);
        }
    }

    public class TestDynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
        : DynamicEntityMapper(serviceProvider, rqlMapAccessor)
    {
        public bool UseAssignForPlatformEntitiesOverride { get; set; }
        public bool EnsureCollectionLoadedAsyncResult { get; set; } = true;
        public object? FindEntityAsyncResult { get; set; }

        public List<(object declaringObject, PropertyInfo property, object? entity)> UpdatePlatformEntityReferenceCalls { get; } = [];

        protected internal override bool UseAssignForPlatformEntities => UseAssignForPlatformEntitiesOverride;

        protected internal override Task<object?> FindEntityAsync(Type entityType, object entity)
            => Task.FromResult(FindEntityAsyncResult);

        protected internal override Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty)
            => Task.FromResult(EnsureCollectionLoadedAsyncResult);

        protected internal override Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity)
        {
            UpdatePlatformEntityReferenceCalls.Add((declaringObject, property, entity));
            return Task.FromResult(1);
        }

        protected internal override Task EnsureEntityRemovedAsync(object entity)
            => Task.CompletedTask;
    }
}
