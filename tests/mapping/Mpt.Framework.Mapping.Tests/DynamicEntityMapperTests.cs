using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class DynamicEntityMapperTests
{
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly TestDynamicEntityMapper _mapper;
    private readonly ServiceProvider _serviceProvider;

    public DynamicEntityMapperTests()
    {
        var services = new ServiceCollection();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(DynamicEntityMapperTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
        _rqlMapAccessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        _mapper = new TestDynamicEntityMapper(_serviceProvider, _rqlMapAccessor);
    }

    [Fact]
    public async Task MapPrimitiveAsync_ShouldCallMapInternalWithPrimitiveModeOnly()
    {
        var source = new TestEntity { Id = "1", Name = "Test" };
        var target = new TestDbEntity { Id = "1", Name = "Old" };

        await _mapper.MapPrimitiveAsync(source, target);

        target.Name.Should().Be("Test");
        target.ComplexProperty.Should().BeNull();
    }

    [Fact]
    public async Task MapComplexAsync_ShouldCallMapInternalWithComplexModeOnly()
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

    [Fact]
    public async Task MapAsync_ShouldCallMapInternalWithAllModes()
    {
        var source = new TestEntity
        {
            Id = "1",
            Name = "Test",
            ComplexProperty = new TestComplexProperty { Value = "Complex" },
        };
        var target = new TestDbEntity { Id = "1", Name = "Old" };

        await _mapper.MapAsync(source, target);

        target.Name.Should().Be("Test");
        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("Complex");
    }

    [Fact]
    public async Task ProcessReference_WithNullValue_ShouldSetPropertyToNull()
    {
        var source = new TestEntity { Id = "1", ComplexProperty = null };
        var target = new TestDbEntity
        {
            Id = "1",
            ComplexProperty = new TestComplexProperty { Value = "Existing" },
        };

        await _mapper.MapAsync(source, target);

        target.ComplexProperty.Should().BeNull();
    }

    [Fact]
    public async Task ProcessReference_WithPlatformEntity_WhenUseAssignForPlatformEntitiesTrue_ShouldCallUpdatePlatformEntityReference()
    {
        var source = new TestEntityWithPlatformProperty
        {
            Id = "1",
            PlatformEntity = new TestPlatformEntity { Id = "platform-1" },
        };
        var target = new TestDbEntityWithPlatformProperty { Id = "1" };

        _mapper.UseAssignForPlatformEntitiesOverride = true;

        await _mapper.MapAsync(source, target);

        _mapper.UpdatePlatformEntityReferenceCalls.Should().HaveCount(1);
        _mapper.UpdatePlatformEntityReferenceCalls[0].entity.Should().Be(source.PlatformEntity);
    }

    [Fact]
    public async Task ProcessReference_WithComplexProperty_ShouldCreateNewInstanceAndMapRecursively()
    {
        var source = new TestEntity
        {
            Id = "1",
            ComplexProperty = new TestComplexProperty { Value = "NewValue" },
        };
        var target = new TestDbEntity { Id = "1" };

        await _mapper.MapAsync(source, target);

        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("NewValue");
    }

    [Fact]
    public async Task ProcessCollection_WithNullValue_ShouldSetPropertyToNull()
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
    public async Task ProcessCollection_WhenCollectionCannotBeLoaded_ShouldSkipUpdate()
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

    [Theory]
    [InlineData(null, null)]
    [InlineData("value", "value")]
    [InlineData("old", "new")]
    [InlineData("old", null)]
    public async Task ProcessPrimitive_ShouldUpdatePropertyCorrectly(string? oldValue, string? newValue)
    {
        var source = new TestEntity { Id = "1", Name = newValue };
        var target = new TestDbEntity { Id = "1", Name = oldValue };

        await _mapper.MapAsync(source, target);

        target.Name.Should().Be(newValue);
    }

    [Fact]
    public async Task MapInternalAsync_WithDeferredComplexProperties_ShouldProcessInCorrectOrder()
    {
        var source = new TestEntity
        {
            Id = "1",
            Name = "Test",
            ComplexProperty = new TestComplexProperty { Value = "Complex" },
        };
        var target = new TestDbEntity { Id = "1" };

        await _mapper.MapAsync(source, target);

        target.Name.Should().Be("Test");
        target.ComplexProperty.Should().NotBeNull();
        target.ComplexProperty!.Value.Should().Be("Complex");
    }

    public class TestEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public DateTime? Date { get; set; }
        public TestComplexProperty? ComplexProperty { get; set; }
    }

    public class TestDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public DateTime? Date { get; set; }
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
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
            context.MapDynamic(dest => dest.Date, src => src.Date);
            context.MapDynamic(dest => dest.ComplexProperty, src => src.ComplexProperty);
        }
    }

    public class TestComplexPropertyMap : IRqlMapper<TestComplexProperty, TestComplexProperty>
    {
        public void MapEntity(IRqlMapperContext<TestComplexProperty, TestComplexProperty> context)
        {
            context.MapDynamic(dest => dest.Value, src => src.Value);
        }
    }

    public class TestEntityWithCollectionMap : IRqlMapper<TestDbEntityWithCollection, TestEntityWithCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithCollection, TestEntityWithCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Items, src => src.Items);
        }
    }

    public class TestEntityWithPlatformPropertyMap : IRqlMapper<TestDbEntityWithPlatformProperty, TestEntityWithPlatformProperty>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPlatformProperty, TestEntityWithPlatformProperty> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.PlatformEntity, src => src.PlatformEntity);
        }
    }

    public class TestDynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
        : DynamicEntityMapper(serviceProvider, rqlMapAccessor)
    {
        public bool UseAssignForPlatformEntitiesOverride { get; set; }
        public bool EnsureCollectionLoadedAsyncResult { get; set; } = true;
        public object? FindEntityAsyncResult { get; set; }

        public List<(object declaringObject, PropertyInfo property, object? entity)> UpdatePlatformEntityReferenceCalls { get; } = [];
        public List<object> EnsureEntityRemovedAsyncCalls { get; } = [];

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
        {
            EnsureEntityRemovedAsyncCalls.Add(entity);
            return Task.CompletedTask;
        }
    }
}
