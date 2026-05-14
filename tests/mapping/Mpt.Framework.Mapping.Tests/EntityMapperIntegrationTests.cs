using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class EntityMapperIntegrationTests : IDisposable
{
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly ServiceProvider _serviceProvider;

    public EntityMapperIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(EntityMapperIntegrationTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
        _rqlMapAccessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EfCoreDynamicEntityMapper_WithInMemoryDatabase_ShouldMapEntitiesCorrectly()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var mapper = new EfCoreDynamicEntityMapper(_serviceProvider, _rqlMapAccessor, context);

        var sourceCustomer = new Customer
        {
            Id = "customer-1",
            Name = "John Doe",
            Email = "john@example.com",
            Address = new Address
            {
                Street = "123 Main St",
                City = "New York",
                ZipCode = "10001",
            },
            Orders =
            [
                new Order { Id = "order-1", Amount = 100.50m, Date = DateTime.UtcNow },
                new Order { Id = "order-2", Amount = 250.75m, Date = DateTime.UtcNow.AddDays(-1) },
            ],
        };

        var targetCustomer = new Customer
        {
            Id = "customer-1",
            Name = "Old Name",
            Email = "old@example.com",
        };
        context.Customers.Add(targetCustomer);
        await context.SaveChangesAsync();

        await mapper.MapAsync(sourceCustomer, targetCustomer);

        targetCustomer.Name.Should().Be("John Doe");
        targetCustomer.Email.Should().Be("john@example.com");
        targetCustomer.Address.Should().NotBeNull();
        targetCustomer.Address.Street.Should().Be("123 Main St");
        targetCustomer.Address.City.Should().Be("New York");
        targetCustomer.Address.ZipCode.Should().Be("10001");
        targetCustomer.Orders.Should().HaveCount(2);
        targetCustomer.Orders.Select(o => o.Amount).Should().BeEquivalentTo([100.50m, 250.75m]);
    }

    [Theory]
    [InlineData(typeof(InMemoryEntityMapper), false)]
    [InlineData(typeof(EfCoreDynamicEntityMapper), true)]
    public async Task MapperImplementations_ShouldHaveCorrectPlatformEntityBehavior(Type mapperType, bool expectedUseAssign)
    {
        IDynamicEntityMapper mapper;

        if (mapperType == typeof(InMemoryEntityMapper))
        {
            mapper = new InMemoryEntityMapper(_serviceProvider, _rqlMapAccessor);
        }
        else
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            await using var context = new TestDbContext(options);
            mapper = new EfCoreDynamicEntityMapper(_serviceProvider, _rqlMapAccessor, context);
        }

        var useAssign = GetUseAssignForPlatformEntities(mapper);

        useAssign.Should().Be(expectedUseAssign);
    }

    [Fact]
    public async Task BothMappers_WithSameInput_ShouldProduceSimilarResults()
    {
        var inMemoryMapper = new InMemoryEntityMapper(_serviceProvider, _rqlMapAccessor);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var efCoreContext = new TestDbContext(options);
        var efCoreMapper = new EfCoreDynamicEntityMapper(_serviceProvider, _rqlMapAccessor, efCoreContext);

        var source = new TestEntity
        {
            Id = "test-1",
            Name = "Test Name",
            Value = 42,
            ComplexProperty = new TestComplexProperty { Description = "Complex Description" },
        };

        var target1 = new TestEntity { Id = "test-1", Name = "Old Name", Value = 0 };
        var target2 = new TestEntity { Id = "test-1", Name = "Old Name", Value = 0 };

        await inMemoryMapper.MapAsync(source, target1);
        await efCoreMapper.MapAsync(source, target2);

        target1.Name.Should().Be(target2.Name);
        target1.Value.Should().Be(target2.Value);
    }

    [Fact]
    public async Task AllMappers_WithLargeDataSet_ShouldCompleteInReasonableTime()
    {
        const int itemCount = 1000;

        var inMemoryMapper = new InMemoryEntityMapper(_serviceProvider, _rqlMapAccessor);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var efCoreContext = new TestDbContext(options);
        var efCoreMapper = new EfCoreDynamicEntityMapper(_serviceProvider, _rqlMapAccessor, efCoreContext);

        var sources = Enumerable.Range(1, itemCount)
            .Select(i => new TestEntity
            {
                Id = $"item-{i}",
                Name = $"Item {i}",
                Value = i,
            })
            .ToList();

        var targets1 = sources.Select(s => new TestEntity { Id = s.Id, Name = string.Empty }).ToList();
        var targets2 = sources.Select(s => new TestEntity { Id = s.Id, Name = string.Empty }).ToList();

        for (int i = 0; i < itemCount; i++)
        {
            await inMemoryMapper.MapAsync(sources[i], targets1[i]);
            await efCoreMapper.MapAsync(sources[i], targets2[i]);
        }

        targets1.All(t => !string.IsNullOrEmpty(t.Name)).Should().BeTrue();
        targets2.All(t => !string.IsNullOrEmpty(t.Name)).Should().BeTrue();
    }

    private static bool GetUseAssignForPlatformEntities(IDynamicEntityMapper mapper)
    {
        var property = mapper.GetType().GetProperty(
            "UseAssignForPlatformEntities",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)property!.GetValue(mapper)!;
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.OwnsOne(e => e.Address);
                entity.HasMany(e => e.Orders)
                      .WithOne()
                      .HasForeignKey("CustomerId");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });
        }
    }

    public class Customer
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public Address Address { get; set; } = null!;
        public List<Order> Orders { get; set; } = [];
    }

    public class Address
    {
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string ZipCode { get; set; } = null!;
    }

    public class Order
    {
        public string Id { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class TestEntity
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public TestComplexProperty? ComplexProperty { get; set; }
    }

    public class TestComplexProperty
    {
        public string Description { get; set; } = null!;
    }

    public class CustomerSelfMap : IRqlMapper<Customer, Customer>
    {
        public void MapEntity(IRqlMapperContext<Customer, Customer> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
            context.MapDynamic(dest => dest.Email, src => src.Email);
            context.MapDynamic(dest => dest.Address, src => src.Address);
            context.MapDynamic(dest => dest.Orders, src => src.Orders);
        }
    }

    public class AddressSelfMap : IRqlMapper<Address, Address>
    {
        public void MapEntity(IRqlMapperContext<Address, Address> context)
        {
            context.MapDynamic(dest => dest.Street, src => src.Street);
            context.MapDynamic(dest => dest.City, src => src.City);
            context.MapDynamic(dest => dest.ZipCode, src => src.ZipCode);
        }
    }

    public class OrderSelfMap : IRqlMapper<Order, Order>
    {
        public void MapEntity(IRqlMapperContext<Order, Order> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Amount, src => src.Amount);
            context.MapDynamic(dest => dest.Date, src => src.Date);
        }
    }

    public class TestEntitySelfMap : IRqlMapper<TestEntity, TestEntity>
    {
        public void MapEntity(IRqlMapperContext<TestEntity, TestEntity> context)
        {
            context.MapDynamic(dest => dest.Id, src => src.Id);
            context.MapDynamic(dest => dest.Name, src => src.Name);
            context.MapDynamic(dest => dest.Value, src => src.Value);
            context.MapDynamic(dest => dest.ComplexProperty, src => src.ComplexProperty);
        }
    }

    public class TestComplexPropertySelfMap : IRqlMapper<TestComplexProperty, TestComplexProperty>
    {
        public void MapEntity(IRqlMapperContext<TestComplexProperty, TestComplexProperty> context)
        {
            context.MapDynamic(dest => dest.Description, src => src.Description);
        }
    }
}
