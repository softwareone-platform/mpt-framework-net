using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class InMemoryEntityMapperTests
{
    private readonly IServiceProvider _services;

    public InMemoryEntityMapperTests()
    {
        var services = new ServiceCollection();
        services.AddRql(c => c.ScanForMappers(Assembly.GetExecutingAssembly()));
        services.AddInMemoryMapping();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task MapAsync_WhenPrimitivesDiffer_UpdatesInPlaceAndReportsCount()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity { Id = "1", Name = "old", Count = 1 };
        var view = new ViewEntity { Id = "1", Name = "new", Count = 2 };

        var changed = await mapper.MapAsync(view, db);

        changed.Should().Be(2);
        db.Name.Should().Be("new");
        db.Count.Should().Be(2);
    }

    [Fact]
    public async Task MapAsync_WhenPrimitivesIdentical_ReportsZero()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity { Id = "1", Name = "same", Count = 5 };
        var view = new ViewEntity { Id = "1", Name = "same", Count = 5 };

        var changed = await mapper.MapAsync(view, db);

        changed.Should().Be(0);
    }

    [Fact]
    public async Task MapPrimitiveAsync_DoesNotTouchCollectionProperties()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity { Id = "1", Name = "old", Tags = ["existing"] };
        var view = new ViewEntity { Id = "1", Name = "new", Tags = ["a", "b"] };

        var changed = await mapper.MapPrimitiveAsync(view, db);

        changed.Should().Be(1);
        db.Name.Should().Be("new");
        db.Tags.Should().Equal("existing");
    }

    [Fact]
    public async Task MapComplexAsync_OnlyTouchesCollectionAndReferenceProperties()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity { Id = "1", Name = "old", Tags = ["existing"] };
        var view = new ViewEntity { Id = "1", Name = "new", Tags = ["a", "b"] };

        var changed = await mapper.MapComplexAsync(view, db);

        changed.Should().Be(1);
        db.Name.Should().Be("old");
        db.Tags.Should().Equal("a", "b");
    }

    [Fact]
    public async Task MapPathAsync_StopsProcessingAfterPathTarget()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity { Id = "1", Name = "old", Count = 1, Tags = ["existing"] };
        var view = new ViewEntity { Id = "1", Name = "new", Count = 2, Tags = ["a", "b"] };

        // MapPath processes entries in declaration order up to and including the path
        // target, then stops. Properties declared after the target (Tags here) stay untouched.
        var changed = await mapper.MapPathAsync(view, v => v.Count, db);

        db.Count.Should().Be(2);
        db.Tags.Should().Equal("existing");
        changed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MapAsync_WithPlatformObjectCollection_UpdatesExistingItemsInPlace()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var existingChild = new DbChild { Id = "c1", Label = "old" };
        var db = new DbEntity { Id = "1", Name = "n", Children = [existingChild] };
        var view = new ViewEntity { Id = "1", Name = "n", Children = [new ViewChild { Id = "c1", Label = "new" }] };

        await mapper.MapAsync(view, db);

        db.Children.Should().ContainSingle();
        db.Children[0].Should().BeSameAs(existingChild);
        db.Children[0].Label.Should().Be("new");
    }

    [Fact]
    public async Task MapAsync_WithPlatformObjectCollection_RemovesItemsNoLongerPresent()
    {
        var mapper = _services.GetRequiredService<IInMemoryEntityMapper>();
        var db = new DbEntity
        {
            Id = "1",
            Children =
            [
                new DbChild { Id = "c1", Label = "one" },
                new DbChild { Id = "c2", Label = "two" },
            ],
        };
        var view = new ViewEntity
        {
            Id = "1",
            Children = [new ViewChild { Id = "c1", Label = "one" }],
        };

        await mapper.MapAsync(view, db);

        db.Children.Should().ContainSingle();
        db.Children[0].Id.Should().Be("c1");
    }

    [Fact]
    public void DI_ResolvesInMemoryEntityMapperAsBothInterfaces()
    {
        using var scope = _services.CreateScope();
        var asDynamic = scope.ServiceProvider.GetRequiredService<IDynamicEntityMapper>();
        var asInMemory = scope.ServiceProvider.GetRequiredService<IInMemoryEntityMapper>();

        asDynamic.Should().BeOfType<InMemoryEntityMapper>();
        asInMemory.Should().BeSameAs(asDynamic);
    }

    public class DbEntity : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<DbChild> Children { get; set; } = [];
    }

    public class DbChild : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ViewEntity : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<ViewChild> Children { get; set; } = [];
    }

    public class ViewChild : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class EntityMap : IRqlMapper<DbEntity, ViewEntity>
    {
        public void MapEntity(IRqlMapperContext<DbEntity, ViewEntity> context)
        {
            context.MapStatic(v => v.Id, d => d.Id);
            context.MapStatic(v => v.Name, d => d.Name);
            context.MapStatic(v => v.Count, d => d.Count);
            context.MapStatic(v => v.Tags, d => d.Tags);
            context.MapDynamic(v => v.Children, d => d.Children);
        }
    }

    public class ChildMap : IRqlMapper<DbChild, ViewChild>
    {
        public void MapEntity(IRqlMapperContext<DbChild, ViewChild> context)
        {
            context.MapStatic(v => v.Id, d => d.Id);
            context.MapStatic(v => v.Label, d => d.Label);
        }
    }
}
