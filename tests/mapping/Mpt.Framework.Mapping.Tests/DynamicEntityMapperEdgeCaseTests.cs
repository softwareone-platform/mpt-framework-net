using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class DynamicEntityMapperEdgeCaseTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDynamicEntityMapper _mapper;

    public DynamicEntityMapperEdgeCaseTests()
    {
        var services = new ServiceCollection();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(DynamicEntityMapperEdgeCaseTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
        var rqlMapAccessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        _mapper = new TestDynamicEntityMapper(_serviceProvider, rqlMapAccessor);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MapAsync_WithPrimitiveCollection_ShouldReplaceAllItems()
    {
        var source = new TestEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["new1", "new2", "new3"],
        };

        var target = new TestDbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["old1", "old2"],
        };

        await _mapper.MapAsync(source, target);

        target.Items.Should().BeEquivalentTo(["new1", "new2", "new3"]);
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityCollection_ShouldHandleCollectionCorrectly()
    {
        var platformEntity1 = new TestPlatformEntity { Id = "p1" };
        var platformEntity2 = new TestPlatformEntity { Id = "p2" };

        var source = new TestEntityWithPlatformCollection
        {
            Id = "1",
            Entities = [platformEntity1, platformEntity2],
        };

        var target = new TestDbEntityWithPlatformCollection
        {
            Id = "1",
            Entities = [],
        };

        await _mapper.MapAsync(source, target);

        target.Entities.Should().HaveCount(2);
        target.Entities.Should().Contain(e => e.Id == "p1");
        target.Entities.Should().Contain(e => e.Id == "p2");
    }

    [Fact]
    public async Task MapAsync_WithComplexCollection_ShouldMapNestedProperties()
    {
        var source = new TestEntityWithComplexCollection
        {
            Id = "1",
            ComplexItems =
            [
                new TestComplexItem { Id = "c1", Value = "value1" },
                new TestComplexItem { Id = "c2", Value = "value2" },
            ],
        };

        var target = new TestDbEntityWithComplexCollection
        {
            Id = "1",
            ComplexItems = [],
        };

        await _mapper.MapAsync(source, target);

        target.ComplexItems.Should().HaveCount(2);
        target.ComplexItems.Should().Contain(c => c.Id == "c1" && c.Value == "value1");
        target.ComplexItems.Should().Contain(c => c.Id == "c2" && c.Value == "value2");
    }

    [Fact]
    public async Task MapAsync_WithNullCollection_ShouldHandleGracefully()
    {
        var source = new TestEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = null!,
        };

        var target = new TestDbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["existing"],
        };

        var act = async () => await _mapper.MapAsync(source, target);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MapAsync_WithEmptyCollection_ShouldClearTargetCollection()
    {
        var source = new TestEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = [],
        };

        var target = new TestDbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["existing1", "existing2"],
        };

        await _mapper.MapAsync(source, target);

        target.Items.Should().BeEmpty();
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
        var source = new TestEntity { Id = "1" };

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
    public async Task MapAsync_WithNullNestedPath_ShouldCreateNested()
    {
        var source = new TestEntityWithPathMapping { Description = "abc" };
        var target = new TestDbEntityWithPathMapping();

        await _mapper.MapAsync(source, target);

        target.Nested.Should().NotBeNull();
        target.Nested!.Description.Should().Be("abc");
    }

    [Fact]
    public async Task MapAsync_WithValueInNestedPath_ShouldUpdateNested()
    {
        var source = new TestEntityWithPathMapping { Description = "abc" };
        var target = new TestDbEntityWithPathMapping
        {
            Nested = new TestDbEntityWithPathMapping.NestedObject { Description = "zyx" },
        };

        await _mapper.MapAsync(source, target);

        target.Nested.Should().NotBeNull();
        target.Nested!.Description.Should().Be("abc");
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityProperties_ShouldAttemptMapping()
    {
        _mapper.UseAssignForPlatformEntitiesOverride = true;

        var platformEntity = new TestPlatformEntity { Id = "p1" };
        var source = new TestEntityWithPlatformReference { Id = "1", Platform = platformEntity };
        var target = new TestDbEntityWithPlatformReference { Id = "1", Platform = null };

        await _mapper.MapAsync(source, target);

        target.Should().NotBeNull();
        target.Id.Should().Be("1");
    }

    [Fact]
    public async Task MapAsync_WithSimpleCircularReference_ShouldHandleGracefully()
    {
        var source = new TestEntity { Id = "parent", Name = "Parent" };
        var target = new TestDbEntity { Id = "parent", Name = "OldParent" };

        var act = async () => await _mapper.MapAsync(source, target);
        await act.Should().NotThrowAsync();

        target.Name.Should().Be("Parent");
    }

    public class TestEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
    }

    public class TestDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
    }

    public class TestEntityWithPrimitiveCollection
    {
        public string Id { get; set; } = null!;
        public List<string> Items { get; set; } = [];
    }

    public class TestDbEntityWithPrimitiveCollection
    {
        public string Id { get; set; } = null!;
        public List<string> Items { get; set; } = [];
    }

    public class TestDbEntityWithPathMapping
    {
        public NestedObject? Nested { get; set; }

        public class NestedObject
        {
            public string Description { get; set; } = null!;
        }
    }

    public class TestEntityWithPathMapping
    {
        public string Description { get; set; } = null!;
    }

    public class TestPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = null!;
        public int Revision { get; set; }
    }

    public class TestEntityWithPlatformCollection
    {
        public string Id { get; set; } = null!;
        public List<TestPlatformEntity> Entities { get; set; } = [];
    }

    public class TestDbEntityWithPlatformCollection
    {
        public string Id { get; set; } = null!;
        public List<TestPlatformEntity> Entities { get; set; } = [];
    }

    public class TestEntityWithPlatformReference
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? Platform { get; set; }
    }

    public class TestDbEntityWithPlatformReference
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? Platform { get; set; }
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

    public class TestEntityToDbMap : IRqlMapper<TestDbEntity, TestEntity>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntity, TestEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
        }
    }

    public class TestEntityWithPrimitiveCollectionMap : IRqlMapper<TestDbEntityWithPrimitiveCollection, TestEntityWithPrimitiveCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPrimitiveCollection, TestEntityWithPrimitiveCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Items, src => src.Items);
        }
    }

    public class TestEntityWithPathMappingMap : IRqlMapper<TestDbEntityWithPathMapping, TestEntityWithPathMapping>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPathMapping, TestEntityWithPathMapping> context)
        {
            context.MapDynamic(t => t.Description, t => t.Nested!.Description);
        }
    }

    public class TestPlatformEntityMap : IRqlMapper<TestPlatformEntity, TestPlatformEntity>
    {
        public void MapEntity(IRqlMapperContext<TestPlatformEntity, TestPlatformEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Revision, src => src.Revision);
        }
    }

    public class TestEntityWithPlatformCollectionMap : IRqlMapper<TestDbEntityWithPlatformCollection, TestEntityWithPlatformCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPlatformCollection, TestEntityWithPlatformCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Entities, src => src.Entities);
        }
    }

    public class TestEntityWithPlatformReferenceMap : IRqlMapper<TestDbEntityWithPlatformReference, TestEntityWithPlatformReference>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithPlatformReference, TestEntityWithPlatformReference> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Platform, src => src.Platform);
        }
    }

    public class TestComplexItemMap : IRqlMapper<TestComplexItem, TestComplexItem>
    {
        public void MapEntity(IRqlMapperContext<TestComplexItem, TestComplexItem> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Value, src => src.Value);
        }
    }

    public class TestEntityWithComplexCollectionMap : IRqlMapper<TestDbEntityWithComplexCollection, TestEntityWithComplexCollection>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntityWithComplexCollection, TestEntityWithComplexCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.ComplexItems, src => src.ComplexItems);
        }
    }

    public class TestDynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
        : DynamicEntityMapper(serviceProvider, rqlMapAccessor)
    {
        public bool UseAssignForPlatformEntitiesOverride { get; set; }
        public bool EnsureCollectionLoadedAsyncResult { get; set; } = true;

        public List<(string id, object entity)> FindEntityAsyncCalls { get; } = [];

        protected internal override bool UseAssignForPlatformEntities => UseAssignForPlatformEntitiesOverride;

        protected internal override Task<object?> FindEntityAsync(Type entityType, object entity)
        {
            if (entity is IPlatformEntity platformEntity)
            {
                var found = FindEntityAsyncCalls.FirstOrDefault(c => c.id == platformEntity.Id);
                return Task.FromResult<object?>(found.entity);
            }
            return Task.FromResult<object?>(null);
        }

        protected internal override Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty)
            => Task.FromResult(EnsureCollectionLoadedAsyncResult);

        protected internal override Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity)
            => Task.FromResult(1);

        protected internal override Task EnsureEntityRemovedAsync(object entity)
            => Task.CompletedTask;
    }
}
