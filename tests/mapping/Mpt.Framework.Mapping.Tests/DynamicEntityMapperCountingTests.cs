using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class DynamicEntityMapperCountingTests
{
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly TestCountingMapper _mapper;
    private readonly ServiceProvider _serviceProvider;

    public DynamicEntityMapperCountingTests()
    {
        var services = new ServiceCollection();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(DynamicEntityMapperCountingTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
        _rqlMapAccessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        _mapper = new TestCountingMapper(_serviceProvider, _rqlMapAccessor);
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithNoChanges_ReturnsZero()
    {
        var source = new SimpleEntity { Id = "1", Name = "Test", Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = "Test", Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithSinglePropertyChange_ReturnsOne()
    {
        var source = new SimpleEntity { Id = "1", Name = "NewName", Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = "OldName", Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(1);
        target.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithMultiplePropertyChanges_ReturnsCorrectCount()
    {
        var source = new SimpleEntity { Id = "1", Name = "NewName", Value = 100 };
        var target = new SimpleDbEntity { Id = "1", Name = "OldName", Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(2);
        target.Name.Should().Be("NewName");
        target.Value.Should().Be(100);
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithNullToValue_ReturnsOne()
    {
        var source = new SimpleEntity { Id = "1", Name = "Test", Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = null, Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(1);
        target.Name.Should().Be("Test");
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithValueToNull_ReturnsOne()
    {
        var source = new SimpleEntity { Id = "1", Name = null, Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = "Test", Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(1);
        target.Name.Should().BeNull();
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithNullToNull_ReturnsZero()
    {
        var source = new SimpleEntity { Id = "1", Name = null, Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = null, Value = 42 };

        var updateCount = await _mapper.MapPrimitiveAsync(source, target);

        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapComplexAsync_WithNewComplexObject_ReturnsCorrectCount()
    {
        var source = new EntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "Test", Number = 42 },
        };
        var target = new DbEntityWithComplex { Id = "1", Complex = null };

        var updateCount = await _mapper.MapComplexAsync(source, target);

        updateCount.Should().Be(3);
        target.Complex.Should().NotBeNull();
        target.Complex!.Value.Should().Be("Test");
        target.Complex.Number.Should().Be(42);
    }

    [Fact]
    public async Task MapComplexAsync_WithExistingComplexObject_ReturnsCorrectCount()
    {
        var source = new EntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "NewValue", Number = 100 },
        };
        var target = new DbEntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "OldValue", Number = 42 },
        };

        var updateCount = await _mapper.MapComplexAsync(source, target);

        updateCount.Should().Be(2);
        target.Complex!.Value.Should().Be("NewValue");
        target.Complex.Number.Should().Be(100);
    }

    [Fact]
    public async Task MapComplexAsync_WithNoChangesInComplexObject_ReturnsZero()
    {
        var source = new EntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "Test", Number = 42 },
        };
        var target = new DbEntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "Test", Number = 42 },
        };

        var updateCount = await _mapper.MapComplexAsync(source, target);

        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapComplexAsync_WithComplexObjectToNull_ReturnsOne()
    {
        var source = new EntityWithComplex { Id = "1", Complex = null };
        var target = new DbEntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "Test", Number = 42 },
        };

        var updateCount = await _mapper.MapComplexAsync(source, target);

        updateCount.Should().Be(1);
        target.Complex.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_WithPrimitiveCollectionChanges_ReturnsOne()
    {
        var source = new EntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["item1", "item2", "item3"],
        };
        var target = new DbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["oldItem1", "oldItem2"],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(1);
        target.Items.Should().Equal("item1", "item2", "item3");
    }

    [Fact]
    public async Task MapAsync_WithPrimitiveCollectionNoChanges_ReturnsZero()
    {
        var source = new EntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["item1", "item2"],
        };
        var target = new DbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["item1", "item2"],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapAsync_WithEmptyPrimitiveCollection_ReturnsOne()
    {
        var source = new EntityWithPrimitiveCollection
        {
            Id = "1",
            Items = [],
        };
        var target = new DbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["item1", "item2"],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(1);
        target.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task MapAsync_WithComplexCollectionAdditions_ReturnsCorrectCount()
    {
        var source = new EntityWithComplexCollection
        {
            Id = "1",
            Items =
            [
                new ComplexItem { Id = "1", Name = "Item1" },
                new ComplexItem { Id = "2", Name = "Item2" },
            ],
        };
        var target = new DbEntityWithComplexCollection
        {
            Id = "1",
            Items = [],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(2);
        target.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityCollection_ReturnsCorrectCount()
    {
        _mapper.UseAssignForPlatformEntitiesOverride = true;
        _mapper.FindEntityAsyncResult = new TestPlatformEntity { Id = "1" };

        var source = new EntityWithPlatformCollection
        {
            Id = "1",
            Entities =
            [
                new TestPlatformEntity { Id = "1" },
                new TestPlatformEntity { Id = "2" },
            ],
        };
        var target = new DbEntityWithPlatformCollection
        {
            Id = "1",
            Entities = [],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(2);
        target.Entities.Should().HaveCount(2);
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityReference_ReturnsOne()
    {
        _mapper.UseAssignForPlatformEntitiesOverride = true;
        _mapper.UpdatePlatformEntityReferenceReturnValue = 1;

        var source = new EntityWithPlatformReference
        {
            Id = "1",
            PlatformEntity = new TestPlatformEntity { Id = "platform1" },
        };
        var target = new DbEntityWithPlatformReference
        {
            Id = "1",
            PlatformEntity = null,
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(1);
        _mapper.UpdatePlatformEntityReferenceCalls.Should().HaveCount(1);
    }

    [Fact]
    public async Task MapAsync_WithMixedChanges_ReturnsCorrectTotalCount()
    {
        var source = new FullEntity
        {
            Id = "1",
            Name = "NewName",
            Complex = new ComplexProperty { Value = "NewValue", Number = 100 },
            Items = ["new1", "new2"],
        };
        var target = new FullDbEntity
        {
            Id = "1",
            Name = "OldName",
            Complex = null,
            Items = ["old1"],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(5);
        target.Name.Should().Be("NewName");
        target.Complex.Should().NotBeNull();
        target.Complex!.Value.Should().Be("NewValue");
        target.Complex.Number.Should().Be(100);
        target.Items.Should().Equal("new1", "new2");
    }

    [Fact]
    public async Task MapPathAsync_WithSinglePropertyPath_ReturnsCorrectCount()
    {
        var source = new SimpleEntity { Id = "1", Name = "NewName", Value = 42 };
        var target = new SimpleDbEntity { Id = "1", Name = "OldName", Value = 42 };

        var updateCount = await _mapper.MapPathAsync(source, x => x.Name!, target);

        updateCount.Should().Be(1);
        target.Name.Should().Be("NewName");
        target.Value.Should().Be(42);
    }

    [Fact]
    public async Task MapPathAsync_WithNoChangeInPath_ReturnsZero()
    {
        var source = new SimpleEntity { Id = "1", Name = "SameName", Value = 100 };
        var target = new SimpleDbEntity { Id = "1", Name = "SameName", Value = 42 };

        var updateCount = await _mapper.MapPathAsync(source, x => x.Name!, target);

        updateCount.Should().Be(0);
        target.Name.Should().Be("SameName");
        target.Value.Should().Be(42);
    }

    [Fact]
    public async Task MapPathAsync_WithComplexPropertyPath_ReturnsCorrectCount()
    {
        var source = new EntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "NewValue", Number = 100 },
        };
        var target = new DbEntityWithComplex
        {
            Id = "1",
            Complex = new ComplexProperty { Value = "OldValue", Number = 42 },
        };

        var updateCount = await _mapper.MapPathAsync(source, x => x.Complex!, target);

        updateCount.Should().Be(2);
        target.Complex!.Value.Should().Be("NewValue");
        target.Complex.Number.Should().Be(100);
    }

    [Fact]
    public async Task MapAsync_WithReadOnlyProperty_SkipsPropertyAndReturnsZero()
    {
        var source = new SimpleEntity { Id = "1", Name = "Test", Value = 42 };
        var target = new DbEntityWithReadOnly { Id = "1" };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(0);
        target.ReadOnlyProperty.Should().Be("ReadOnly");
    }

    [Fact]
    public async Task MapAsync_WithCollectionLoadingFailed_ReturnsZero()
    {
        _mapper.EnsureCollectionLoadedAsyncResult = false;

        var source = new EntityWithPrimitiveCollection
        {
            Id = "1",
            Items = ["item1"],
        };
        var target = new DbEntityWithPrimitiveCollection
        {
            Id = "1",
            Items = [],
        };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(0);
        target.Items.Should().BeEmpty();
    }

    public class SimpleEntityToDbEntityMap : IRqlMapper<SimpleDbEntity, SimpleEntity>
    {
        public void MapEntity(IRqlMapperContext<SimpleDbEntity, SimpleEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
            context.MapDynamic(dest => dest.Value, src => src.Value);
        }
    }

    public class EntityWithComplexToDbEntityMap : IRqlMapper<DbEntityWithComplex, EntityWithComplex>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithComplex, EntityWithComplex> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Complex, src => src.Complex);
        }
    }

    public class ComplexPropertyMap : IRqlMapper<ComplexProperty, ComplexProperty>
    {
        public void MapEntity(IRqlMapperContext<ComplexProperty, ComplexProperty> context)
        {
            context.MapDynamic(dest => dest.Value, src => src.Value);
            context.MapDynamic(dest => dest.Number, src => src.Number);
        }
    }

    public class EntityWithPrimitiveCollectionToDbMap : IRqlMapper<DbEntityWithPrimitiveCollection, EntityWithPrimitiveCollection>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithPrimitiveCollection, EntityWithPrimitiveCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Items, src => src.Items);
        }
    }

    public class EntityWithComplexCollectionToDbMap : IRqlMapper<DbEntityWithComplexCollection, EntityWithComplexCollection>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithComplexCollection, EntityWithComplexCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Items, src => src.Items);
        }
    }

    public class ComplexItemMap : IRqlMapper<ComplexItem, ComplexItem>
    {
        public void MapEntity(IRqlMapperContext<ComplexItem, ComplexItem> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
        }
    }

    public class EntityWithPlatformCollectionToDbMap : IRqlMapper<DbEntityWithPlatformCollection, EntityWithPlatformCollection>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithPlatformCollection, EntityWithPlatformCollection> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Entities, src => src.Entities);
        }
    }

    public class EntityWithPlatformReferenceToDbMap : IRqlMapper<DbEntityWithPlatformReference, EntityWithPlatformReference>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithPlatformReference, EntityWithPlatformReference> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.PlatformEntity, src => src.PlatformEntity);
        }
    }

    public class FullEntityToDbMap : IRqlMapper<FullDbEntity, FullEntity>
    {
        public void MapEntity(IRqlMapperContext<FullDbEntity, FullEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
            context.MapDynamic(dest => dest.Complex, src => src.Complex);
            context.MapDynamic(dest => dest.Items, src => src.Items);
        }
    }

    public class SimpleEntityToDbEntityWithReadOnlyMap : IRqlMapper<DbEntityWithReadOnly, SimpleEntity>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithReadOnly, SimpleEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
        }
    }

    public class SimpleEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class SimpleDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class EntityWithComplex
    {
        public string Id { get; set; } = null!;
        public ComplexProperty? Complex { get; set; }
    }

    public class DbEntityWithComplex
    {
        public string Id { get; set; } = null!;
        public ComplexProperty? Complex { get; set; }
    }

    public class ComplexProperty
    {
        public string? Value { get; set; }
        public int Number { get; set; }
    }

    public class EntityWithPrimitiveCollection
    {
        public string Id { get; set; } = null!;
        public List<string>? Items { get; set; }
    }

    public class DbEntityWithPrimitiveCollection
    {
        public string Id { get; set; } = null!;
        public List<string>? Items { get; set; }
    }

    public class EntityWithComplexCollection
    {
        public string Id { get; set; } = null!;
        public List<ComplexItem>? Items { get; set; }
    }

    public class DbEntityWithComplexCollection
    {
        public string Id { get; set; } = null!;
        public List<ComplexItem>? Items { get; set; }
    }

    public class ComplexItem
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
    }

    public class EntityWithPlatformCollection
    {
        public string Id { get; set; } = null!;
        public List<TestPlatformEntity>? Entities { get; set; }
    }

    public class DbEntityWithPlatformCollection
    {
        public string Id { get; set; } = null!;
        public List<TestPlatformEntity>? Entities { get; set; }
    }

    public class EntityWithPlatformReference
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? PlatformEntity { get; set; }
    }

    public class DbEntityWithPlatformReference
    {
        public string Id { get; set; } = null!;
        public TestPlatformEntity? PlatformEntity { get; set; }
    }

    public class FullEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public ComplexProperty? Complex { get; set; }
        public List<string>? Items { get; set; }
    }

    public class FullDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public ComplexProperty? Complex { get; set; }
        public List<string>? Items { get; set; }
    }

    public class DbEntityWithReadOnly
    {
        public string Id { get; set; } = null!;
        public string ReadOnlyProperty { get; init; } = "ReadOnly";
    }

    public class TestPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = null!;
        public int Revision { get; set; }
    }

    public class TestCountingMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
        : DynamicEntityMapper(serviceProvider, rqlMapAccessor)
    {
        public bool UseAssignForPlatformEntitiesOverride { get; set; }
        public bool EnsureCollectionLoadedAsyncResult { get; set; } = true;
        public object? FindEntityAsyncResult { get; set; }
        public int UpdatePlatformEntityReferenceReturnValue { get; set; } = 1;

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
            return Task.FromResult(UpdatePlatformEntityReferenceReturnValue);
        }

        protected internal override Task EnsureEntityRemovedAsync(object entity)
        {
            EnsureEntityRemovedAsyncCalls.Add(entity);
            return Task.CompletedTask;
        }
    }
}
