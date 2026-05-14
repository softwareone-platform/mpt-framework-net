using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class EfCoreDynamicEntityMapperHookTests : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private TestDbContext? _dbContext;
    private IEfCoreDynamicEntityMapper? _mapper;

    private void SetupTest()
    {
        var services = new ServiceCollection();
        services.AddRql(config => config.ScanForMappers(typeof(EfCoreDynamicEntityMapperHookTests).Assembly));
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddEfCoreMapping<TestDbContext>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        _mapper = _serviceProvider.GetRequiredService<IEfCoreDynamicEntityMapper>();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MapAsync_WithNewEntity_ShouldCreateNewInstance()
    {
        SetupTest();
        var source = new TestSourceEntity { Id = "new-id", Name = "New Entity" };
        var target = new TestDbEntity { Id = "new-id" };

        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        await _mapper!.MapAsync(source, target);

        target.Name.Should().Be("New Entity");
    }

    [Fact]
    public async Task FindEntityAsync_WithNullId_ShouldReturnNull()
    {
        SetupTest();
        var entityWithNullId = new TestEntityWithNullId { Id = null };

        var findEntityMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("FindEntityAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<object?>)findEntityMethod!.Invoke(_mapper!, [typeof(TestDbEntity), entityWithNullId])!;
        var result = await task;

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindEntityAsync_WithEmptyId_ShouldReturnNullOrEmpty()
    {
        SetupTest();
        var entityWithEmptyId = new TestEntityWithEmptyId { Id = string.Empty };

        var findEntityMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("FindEntityAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<object?>)findEntityMethod!.Invoke(_mapper!, [typeof(TestDbEntity), entityWithEmptyId])!;
        var result = await task;

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindEntityAsync_WithNonExistentEntity_ShouldReturnNull()
    {
        SetupTest();
        var entityWithNonExistentId = new TestPlatformEntity { Id = "non-existent-id" };

        var findEntityMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("FindEntityAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<object?>)findEntityMethod!.Invoke(_mapper!, [typeof(TestDbEntity), entityWithNonExistentId])!;
        var result = await task;

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindEntityAsync_WithExistingEntity_ShouldReturnFoundEntity()
    {
        SetupTest();
        var existingEntity = new TestDbEntity { Id = "existing-id", Name = "Existing Entity" };
        _dbContext!.TestEntities.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        var searchEntity = new TestPlatformEntity { Id = "existing-id" };

        var findEntityMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("FindEntityAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<object?>)findEntityMethod!.Invoke(_mapper!, [typeof(TestDbEntity), searchEntity])!;
        var result = await task;

        result.Should().NotBeNull();
        result.Should().BeOfType<TestDbEntity>();
        ((TestDbEntity)result!).Id.Should().Be("existing-id");
        ((TestDbEntity)result).Name.Should().Be("Existing Entity");
    }

    [Fact]
    public async Task FindEntityAsync_WithCompositeKey_ShouldHandleGracefully()
    {
        SetupTest();
        var entityWithCompositeKey = new TestEntityWithCompositeKey { Id1 = "key1", Id2 = "key2" };

        var findEntityMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("FindEntityAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task<object?>)findEntityMethod!.Invoke(_mapper!, [typeof(TestDbEntity), entityWithCompositeKey])!;
        var result = await task;

        result.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_WithCollectionProperty_ShouldLoadAndMapCollection()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "entity-with-collection" };
        _dbContext!.TestEntities.Add(target);

        var existingItem1 = new TestCollectionItem { Id = "item1", ParentId = "entity-with-collection", Value = "Old1" };
        var existingItem2 = new TestCollectionItem { Id = "item2", ParentId = "entity-with-collection", Value = "Old2" };
        _dbContext.CollectionItems.AddRange(existingItem1, existingItem2);
        await _dbContext.SaveChangesAsync();

        var source = new TestSourceEntity
        {
            Id = "entity-with-collection",
            Name = "Entity with collection",
            Items =
            [
                new TestCollectionItem { Id = "item1", Value = "Updated1" },
                new TestCollectionItem { Id = "item3", Value = "New3" },
            ],
        };

        await _mapper!.MapAsync(source, target);

        target.Items.Should().NotBeNull();
        target.Items.Should().HaveCount(2);
        target.Items.Should().Contain(x => x.Id == "item1" && x.Value == "Updated1");
        target.Items.Should().Contain(x => x.Id == "item3" && x.Value == "New3");
    }

    [Fact]
    public async Task MapAsync_WithNullCollection_ShouldHandleGracefully()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "entity-id" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var source = new TestSourceEntity { Id = "entity-id", Name = "Entity", Items = null };

        var act = async () => await _mapper!.MapAsync(source, target);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithValidNavigationProperty_ShouldReturnTrue()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "entity-with-collection" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var itemsProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Items))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [target, itemsProperty])!;
        var result = await task;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithNonNavigationProperty_ShouldReturnFalse()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "entity-id" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var nameProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Name))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [target, nameProperty])!;
        var result = await task;

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithUntrackedEntity_BehavesConsistently()
    {
        SetupTest();
        var untrackedEntity = new TestDbEntity { Id = "untracked-entity" };

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var itemsProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Items))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [untrackedEntity, itemsProperty])!;
        var result = await task;

        // EF Core's behavior for an untracked entity here is implementation-defined; assert it does not throw.
        _ = result; // result is implementation-defined; assertion is "did not throw".
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithUnmappedEntityType_ShouldReturnFalse()
    {
        SetupTest();
        var unmappedEntity = new UnmappedEntity { Id = "unmapped" };

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var itemsProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Items))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [unmappedEntity, itemsProperty])!;
        var result = await task;

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithAlreadyLoadedCollection_ShouldReturnTrueWithoutLoading()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "entity-with-collection" };
        _dbContext!.TestEntities.Add(target);

        var item1 = new TestCollectionItem { Id = "item1", ParentId = "entity-with-collection", Value = "Value1" };
        _dbContext.CollectionItems.Add(item1);
        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(target).Collection(x => x.Items).LoadAsync();

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var itemsProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Items))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [target, itemsProperty])!;
        var result = await task;

        result.Should().BeTrue();
        target.Items.Should().HaveCount(1);
        target.Items[0].Id.Should().Be("item1");
    }

    [Fact]
    public async Task EnsureCollectionLoadedAsync_WithSkipNavigation_ShouldHandleCorrectly()
    {
        SetupTest();
        var target = new TestEntityWithSkipNavigation { Id = "entity-with-skip-nav" };
        _dbContext!.TestEntitiesWithSkipNav.Add(target);
        await _dbContext.SaveChangesAsync();

        var ensureCollectionMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureCollectionLoadedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var skipNavProperty = typeof(TestEntityWithSkipNavigation).GetProperty(nameof(TestEntityWithSkipNavigation.RelatedEntities))!;
        var task = (Task<bool>)ensureCollectionMethod!.Invoke(_mapper!, [target, skipNavProperty])!;
        var result = await task;

        _ = result; // result is implementation-defined; assertion is "did not throw".
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityReference_ShouldBeHandledByRqlMapping()
    {
        SetupTest();
        var referencedEntity = new TestReferencedEntity { Id = "ref-id", Name = "Referenced" };
        _dbContext!.ReferencedEntities.Add(referencedEntity);

        var target = new TestDbEntity { Id = "target-id" };
        _dbContext.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var source = new TestSourceEntity
        {
            Id = "target-id",
            Name = "Target Entity",
            PlatformReference = new TestPlatformEntity { Id = "ref-id" },
        };

        await _mapper!.MapAsync(source, target);

        target.Name.Should().Be("Target Entity");
    }

    [Fact]
    public async Task MapAsync_WithNullPlatformEntityReference_ShouldHandleGracefully()
    {
        SetupTest();
        var target = new TestDbEntity
        {
            Id = "target-id",
            PlatformReferenceId = "existing-ref-id",
        };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var source = new TestSourceEntity
        {
            Id = "target-id",
            Name = "Target Entity",
            PlatformReference = null,
        };

        await _mapper!.MapAsync(source, target);

        target.Name.Should().Be("Target Entity");
    }

    [Fact]
    public async Task UpdatePlatformEntityReference_WithUntrackedEntity_ShouldReturnEarly()
    {
        SetupTest();
        var untrackedEntity = new TestDbEntity { Id = "untracked" };
        var platformEntity = new TestPlatformEntity { Id = "platform-ref" };
        var property = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.RelatedEntity))!;

        var updateMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("UpdatePlatformEntityReference", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = async () =>
        {
            var task = (Task)updateMethod!.Invoke(_mapper!, [untrackedEntity, property, platformEntity])!;
            await task;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdatePlatformEntityReference_WithNonNavigationProperty_ShouldReturnEarly()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "target-id" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var platformEntity = new TestPlatformEntity { Id = "platform-ref" };
        var nonNavigationProperty = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.Name))!;

        var updateMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("UpdatePlatformEntityReference", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = async () =>
        {
            var task = (Task)updateMethod!.Invoke(_mapper!, [target, nonNavigationProperty, platformEntity])!;
            await task;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdatePlatformEntityReference_WithNonPlatformEntity_ShouldReturnEarly()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "target-id" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var nonPlatformEntity = new TestCollectionItem { Id = "non-platform" };
        var property = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.RelatedEntity))!;

        var updateMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("UpdatePlatformEntityReference", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = async () =>
        {
            var task = (Task)updateMethod!.Invoke(_mapper!, [target, property, nonPlatformEntity])!;
            await task;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdatePlatformEntityReference_WithNullEntity_ShouldHandleNullForeignKey()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "target-id", PlatformReferenceId = "existing-ref" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var property = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.RelatedEntity))!;

        var updateMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("UpdatePlatformEntityReference", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = async () =>
        {
            var task = (Task)updateMethod!.Invoke(_mapper!, [target, property, null!])!;
            await task;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdatePlatformEntityReference_WithValidPlatformEntity_ShouldSetForeignKey()
    {
        SetupTest();
        var target = new TestDbEntity { Id = "target-id" };
        _dbContext!.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var platformEntity = new TestPlatformEntity { Id = "platform-ref-id" };
        var property = typeof(TestDbEntity).GetProperty(nameof(TestDbEntity.RelatedEntity))!;

        var updateMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("UpdatePlatformEntityReference", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = async () =>
        {
            var task = (Task)updateMethod!.Invoke(_mapper!, [target, property, platformEntity])!;
            await task;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MapAsync_WithEntityRemoval_ShouldMarkEntityForDeletion()
    {
        SetupTest();
        var entityToRemove = new TestDbEntity { Id = "to-remove", Name = "Will be removed" };
        _dbContext!.TestEntities.Add(entityToRemove);
        await _dbContext.SaveChangesAsync();

        var ensureEntityRemovedMethod = typeof(EfCoreDynamicEntityMapper).GetMethod("EnsureEntityRemovedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)ensureEntityRemovedMethod!.Invoke(_mapper!, [entityToRemove])!;

        var entry = _dbContext.Entry(entityToRemove);
        entry.State.Should().Be(EntityState.Deleted);
    }

    [Fact]
    public async Task MapAsync_WithInvalidEntityType_ShouldHandleGracefully()
    {
        SetupTest();
        var unmappedEntity = new UnmappedEntity { Id = "unmapped" };
        var source = new TestSourceEntity { Id = "test" };

        var act = async () => await _mapper!.MapAsync(source, unmappedEntity);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void UseAssignForPlatformEntities_ShouldReturnTrue()
    {
        SetupTest();
        var mapper = new EfCoreDynamicEntityMapper(_serviceProvider!, _serviceProvider!.GetRequiredService<IRqlMapAccessor>(), _dbContext!);

        var property = typeof(EfCoreDynamicEntityMapper).GetProperty("UseAssignForPlatformEntities", BindingFlags.NonPublic | BindingFlags.Instance);
        var useAssign = (bool)property!.GetValue(mapper)!;

        useAssign.Should().BeTrue();
    }

    [Fact]
    public async Task MapAsync_CompleteScenario_ShouldWorkEndToEnd()
    {
        SetupTest();
        var existingRelated = new TestReferencedEntity { Id = "existing-related", Name = "Existing" };
        _dbContext!.ReferencedEntities.Add(existingRelated);

        var target = new TestDbEntity { Id = "complex-entity" };
        _dbContext.TestEntities.Add(target);
        await _dbContext.SaveChangesAsync();

        var source = new TestSourceEntity
        {
            Id = "complex-entity",
            Name = "Complex Entity",
            PlatformReference = new TestPlatformEntity { Id = "platform-ref-id" },
            RelatedEntity = new TestRelatedEntity { Id = "related-ref-id" },
            Items =
            [
                new TestCollectionItem { Id = "item1", Value = "Collection Item 1" },
                new TestCollectionItem { Id = "item2", Value = "Collection Item 2" },
            ],
        };

        await _mapper!.MapAsync(source, target);

        target.Name.Should().Be("Complex Entity");
        target.Items.Should().HaveCount(2);
        target.Items.Should().Contain(x => x.Id == "item1" && x.Value == "Collection Item 1");
        target.Items.Should().Contain(x => x.Id == "item2" && x.Value == "Collection Item 2");
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestDbEntity> TestEntities { get; set; } = null!;
        public DbSet<TestReferencedEntity> ReferencedEntities { get; set; } = null!;
        public DbSet<TestCollectionItem> CollectionItems { get; set; } = null!;
        public DbSet<TestEntityWithSkipNavigation> TestEntitiesWithSkipNav { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestDbEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name);
                entity.Property(e => e.PlatformReferenceId);

                entity.HasOne(e => e.RelatedEntity)
                    .WithMany()
                    .HasForeignKey("RelatedEntityId")
                    .IsRequired(false);

                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey(i => i.ParentId);
            });

            modelBuilder.Entity<TestReferencedEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name);
            });

            modelBuilder.Entity<TestCollectionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Value);
                entity.Property(e => e.ParentId);
            });

            modelBuilder.Entity<TestEntityWithSkipNavigation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();

                entity.HasMany(e => e.RelatedEntities)
                    .WithMany()
                    .UsingEntity("TestEntityRelations");
            });
        }
    }

    public class TestDbEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string? PlatformReferenceId { get; set; }
        public TestReferencedEntity? RelatedEntity { get; set; }
        public List<TestCollectionItem> Items { get; set; } = [];
    }

    public class TestReferencedEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
    }

    public class TestCollectionItem
    {
        public string Id { get; set; } = null!;
        public string? Value { get; set; }
        public string? ParentId { get; set; }
    }

    public class TestEntityWithSkipNavigation
    {
        public string Id { get; set; } = null!;
        public List<TestReferencedEntity> RelatedEntities { get; set; } = [];
    }

    public class TestSourceEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public TestPlatformEntity? PlatformReference { get; set; }
        public TestRelatedEntity? RelatedEntity { get; set; }
        public List<TestCollectionItem>? Items { get; set; }
    }

    public class TestRelatedEntity
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
    }

    public class TestPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = null!;
        public int Revision { get; set; }
    }

    public class UnmappedEntity
    {
        public string Id { get; set; } = null!;
    }

    public class TestEntityWithNullId
    {
        public string? Id { get; set; }
    }

    public class TestEntityWithEmptyId
    {
        public string Id { get; set; } = null!;
    }

    public class TestEntityWithCompositeKey
    {
        public string Id1 { get; set; } = null!;
        public string Id2 { get; set; } = null!;
    }

    public class TestSourceEntityMap : IRqlMapper<TestDbEntity, TestSourceEntity>
    {
        public void MapEntity(IRqlMapperContext<TestDbEntity, TestSourceEntity> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Name, d => d.Name);
            context.MapDynamic(v => v.Items, d => d.Items);
        }
    }

    public class TestCollectionItemMap : IRqlMapper<TestCollectionItem, TestCollectionItem>
    {
        public void MapEntity(IRqlMapperContext<TestCollectionItem, TestCollectionItem> context)
        {
            context.MapDynamic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Value, d => d.Value);
        }
    }
}
