using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class EfCoreDynamicEntityMapperTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly TestDbContext _db;
    private readonly IEfCoreDynamicEntityMapper _mapper;

    public EfCoreDynamicEntityMapperTests()
    {
        var services = new ServiceCollection();
        services.AddRql(c => c.ScanForMappers(Assembly.GetExecutingAssembly()));
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddEfCoreMapping<TestDbContext>();

        _services = services.BuildServiceProvider();
        _db = _services.GetRequiredService<TestDbContext>();
        _mapper = _services.GetRequiredService<IEfCoreDynamicEntityMapper>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _services.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DI_ResolvesEfCoreMapperAsBothInterfaces()
    {
        var asDynamic = _services.GetRequiredService<IDynamicEntityMapper>();
        var asEfCore = _services.GetRequiredService<IEfCoreDynamicEntityMapper>();
        asDynamic.Should().BeOfType<EfCoreDynamicEntityMapper>();
        asEfCore.Should().BeSameAs(asDynamic);
    }

    [Fact]
    public async Task MapAsync_PrimitivesOnly_UpdatesTrackedEntity()
    {
        var db = new ParentDbEntity { Id = "p1", Name = "old" };
        _db.Parents.Add(db);
        await _db.SaveChangesAsync();

        var view = new ParentView { Id = "p1", Name = "new" };

        var changed = await _mapper.MapAsync(view, db);

        changed.Should().Be(1);
        db.Name.Should().Be("new");
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityReference_ReassignsByIdInsteadOfDeepCopying()
    {
        var existingChild = new ChildDbEntity { Id = "c1", Label = "stays" };
        var parent = new ParentDbEntity { Id = "p1", Name = "n" };
        _db.Children.Add(existingChild);
        _db.Parents.Add(parent);
        await _db.SaveChangesAsync();

        // The view carries a different-looking ChildView with the same id — the mapper
        // should look it up in the DbContext and stamp the FK, not try to insert a new row.
        var view = new ParentView
        {
            Id = "p1",
            Name = "n",
            Child = new ChildPlatformEntity { Id = "c1" },
        };

        var changed = await _mapper.MapAsync(view, parent);

        changed.Should().BeGreaterThan(0);
        var entry = _db.Entry(parent);
        entry.Property("ChildId").CurrentValue.Should().Be("c1");
        _db.Children.Local.Should().ContainSingle(c => c.Id == "c1" && c.Label == "stays");
    }

    [Fact]
    public async Task MapAsync_WithPlatformEntityCollection_AddsByLookupAndRemovesViaDbContext()
    {
        var stays = new ChildDbEntity { Id = "c1", Label = "stays" };
        var goesAway = new ChildDbEntity { Id = "c2", Label = "removed" };
        var newcomer = new ChildDbEntity { Id = "c3", Label = "added" };
        var parent = new ParentWithCollectionDbEntity { Id = "p1", Children = [stays, goesAway] };
        _db.ParentsWithCollection.Add(parent);
        _db.Children.Add(newcomer);
        await _db.SaveChangesAsync();

        var view = new ParentWithCollectionView
        {
            Id = "p1",
            Children =
            [
                new ChildPlatformEntity { Id = "c1" },
                new ChildPlatformEntity { Id = "c3" },
            ],
        };

        await _mapper.MapAsync(view, parent);
        await _db.SaveChangesAsync();

        // The mapper looked up c3 by id (not deep-copied a new row), kept c1, and unlinked
        // c2 from the parent. Platform-entity collections are "reassign by id", so removed
        // items stay in the db as standalone rows — the mapper isn't responsible for
        // deleting them; consumers can do that explicitly if they want.
        parent.Children.Select(c => c.Id).Should().BeEquivalentTo(["c1", "c3"]);
        var rowsAfter = await _db.Children.AsNoTracking().Select(c => c.Id).ToListAsync();
        rowsAfter.Should().BeEquivalentTo(["c1", "c2", "c3"]);
    }

    [Fact]
    public async Task MapAsync_WithTrackedParent_KeepsExistingPlatformEntityCollectionItems()
    {
        var parent = new ParentWithCollectionDbEntity { Id = "p1" };
        _db.ParentsWithCollection.Add(parent);
        _db.Children.Add(new ChildDbEntity { Id = "c1", Label = "preexisting", ParentWithCollectionId = "p1" });
        await _db.SaveChangesAsync();

        var view = new ParentWithCollectionView
        {
            Id = "p1",
            Children = [new ChildPlatformEntity { Id = "c1" }],
        };

        await _mapper.MapAsync(view, parent);

        parent.Children.Should().ContainSingle(c => c.Id == "c1" && c.Label == "preexisting");
    }

    [Fact]
    public async Task MapAsync_WithReferenceLookupReturningNull_ThrowsForUnknownPlatformEntity()
    {
        // Source carries a Child id that doesn't exist in the DbContext. ProcessAssignableCollection
        // path calls FindEntityAsync, which returns null, which causes a KeyNotFoundException.
        var parent = new ParentWithCollectionDbEntity { Id = "p1" };
        _db.ParentsWithCollection.Add(parent);
        await _db.SaveChangesAsync();

        var view = new ParentWithCollectionView
        {
            Id = "p1",
            Children = [new ChildPlatformEntity { Id = "missing" }],
        };

        var act = async () => await _mapper.MapAsync(view, parent);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ParentDbEntity> Parents => Set<ParentDbEntity>();
        public DbSet<ParentWithCollectionDbEntity> ParentsWithCollection => Set<ParentWithCollectionDbEntity>();
        public DbSet<ChildDbEntity> Children => Set<ChildDbEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParentDbEntity>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasOne(p => p.Child).WithMany().HasForeignKey("ChildId").IsRequired(false);
            });
            modelBuilder.Entity<ParentWithCollectionDbEntity>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasMany(p => p.Children).WithOne().HasForeignKey(c => c.ParentWithCollectionId).IsRequired(false);
            });
            modelBuilder.Entity<ChildDbEntity>(e => e.HasKey(c => c.Id));
        }
    }

    public class ParentDbEntity : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ChildDbEntity? Child { get; set; }
    }

    public class ParentWithCollectionDbEntity : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public List<ChildDbEntity> Children { get; set; } = [];
    }

    public class ChildDbEntity : IPlatformEntity
    {
        public string Id { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? ParentWithCollectionId { get; set; }
    }

    public class ParentView : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ChildPlatformEntity? Child { get; set; }
    }

    public class ParentWithCollectionView : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public List<ChildPlatformEntity> Children { get; set; } = [];
    }

    public class ChildPlatformEntity : IPlatformEntity
    {
        public string Id { get; set; } = string.Empty;
        public int Revision { get; set; }
    }

    public class ParentMap : IRqlMapper<ParentDbEntity, ParentView>
    {
        public void MapEntity(IRqlMapperContext<ParentDbEntity, ParentView> context)
        {
            context.MapStatic(v => v.Id, d => d.Id);
            context.MapStatic(v => v.Name, d => d.Name);
            context.MapDynamic(v => v.Child, d => d.Child);
        }
    }

    public class ParentWithCollectionMap : IRqlMapper<ParentWithCollectionDbEntity, ParentWithCollectionView>
    {
        public void MapEntity(IRqlMapperContext<ParentWithCollectionDbEntity, ParentWithCollectionView> context)
        {
            context.MapStatic(v => v.Id, d => d.Id);
            context.MapDynamic(v => v.Children, d => d.Children);
        }
    }

    public class ChildMap : IRqlMapper<ChildDbEntity, ChildPlatformEntity>
    {
        public void MapEntity(IRqlMapperContext<ChildDbEntity, ChildPlatformEntity> context)
        {
            context.MapStatic(v => v.Id, d => d.Id);
        }
    }
}
