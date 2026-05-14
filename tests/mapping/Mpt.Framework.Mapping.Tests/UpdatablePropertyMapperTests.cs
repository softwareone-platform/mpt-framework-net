using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Framework.Mapping.Tests;

public class UpdatablePropertyMapperTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRqlMapAccessor _rqlMapAccessor;
    private readonly TestDynamicEntityMapper _mapper;

    public UpdatablePropertyMapperTests()
    {
        var services = new ServiceCollection();

        services.AddScoped<OrderSummaryFactory>();
        services.AddScoped<ProductPriceCalculatorFactory>();
        services.AddScoped<CustomerFullNameFactory>();
        services.AddScoped<ProductReadonlyDisplayFactory>();

        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(UpdatablePropertyMapperTests).Assembly);
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
    public async Task MapAsync_WithFactory_ShouldUpdateEntityUsingFactory()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 5, TotalAmount = 150.50m },
        };

        var target = new Order { Id = "order-1", ItemCount = 0, TotalAmount = 0m };

        var updateCount = await _mapper.MapAsync(source, target);

        target.ItemCount.Should().Be(5);
        target.TotalAmount.Should().Be(150.50m);
        updateCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MapAsync_WithFactoryNoChanges_ShouldReturnZero()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 5, TotalAmount = 150.50m },
        };

        var target = new Order { Id = "order-1", ItemCount = 5, TotalAmount = 150.50m };

        var updateCount = await _mapper.MapAsync(source, target);

        target.ItemCount.Should().Be(5);
        target.TotalAmount.Should().Be(150.50m);
        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapAsync_WithFactoryAndRegularProperties_ShouldMapBoth()
    {
        var source = new ProductDto
        {
            Id = "product-1",
            Name = "Widget",
            PriceInfo = new PriceInfoDto { BasePrice = 100m, Discount = 0.1m },
        };

        var target = new Product
        {
            Id = "product-1",
            Name = "Old Name",
            BasePrice = 0m,
            FinalPrice = 0m,
        };

        var updateCount = await _mapper.MapAsync(source, target);

        target.Name.Should().Be("Widget");
        target.BasePrice.Should().Be(100m);
        target.FinalPrice.Should().Be(90m);
        updateCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MapAsync_WithFactoryAndNullInput_ShouldHandleGracefully()
    {
        var source = new OrderDto { Id = "order-1", Summary = null };
        var target = new Order { Id = "order-1", ItemCount = 10, TotalAmount = 100m };

        var updateCount = await _mapper.MapAsync(source, target);

        target.ItemCount.Should().Be(0);
        target.TotalAmount.Should().Be(0m);
        updateCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MapAsync_WithFactoryAndComplexObject_ShouldPassObjectToFactory()
    {
        var source = new CustomerDto
        {
            Id = "customer-1",
            NameInfo = new NameInfoDto { FirstName = "John", LastName = "Doe" },
        };

        var target = new Customer { Id = "customer-1", FullName = "Old Name" };

        var updateCount = await _mapper.MapAsync(source, target);

        target.FullName.Should().Be("John Doe");
        updateCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void UpdatableMappingFactory_WithInvalidEntityType_ShouldThrowArgumentException()
    {
        var factory = new OrderSummaryFactory();
        var input = new OrderSummaryDto { ItemCount = 1, TotalAmount = 10m };
        var wrongEntity = new Product { Id = "1" };

        var exception = Assert.Throws<ArgumentException>(
            () => ((IUpdatableMappingFactory)factory).TryUpdate(input, wrongEntity, out _));

        exception.Message.Should().Contain("Invalid entity type");
        exception.Message.Should().Contain("Order");
        exception.Message.Should().Contain("Product");
    }

    [Fact]
    public void UpdatableMappingFactory_GetStorageExpression_ShouldReturnValidExpression()
    {
        var factory = new OrderSummaryFactory();

        var expression = factory.GetStorageExpression();

        expression.Should().NotBeNull();
        expression.Parameters.Should().HaveCount(1);
        expression.Parameters[0].Type.Should().Be<Order>();
    }

    [Fact]
    public void UpdatableMappingFactory_GetStorageExpression_ShouldBeCompilable()
    {
        var factory = new OrderSummaryFactory();
        var order = new Order { Id = "1", ItemCount = 5, TotalAmount = 100m };

        var expression = factory.GetStorageExpression();
        var compiledFunc = expression.Compile();
        var result = compiledFunc(order);

        result.Should().NotBeNull();
    }

    [Fact]
    public void UpdatableMappingFactory_Hint_DefaultIsNone()
    {
        var factory = new OrderSummaryFactory();

        factory.Hint.Should().Be(ExpressionFactoryHint.None);
    }

    [Fact]
    public async Task MapComplexAsync_WithFactory_ShouldApplyFactory()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 10, TotalAmount = 250m },
        };

        var target = new Order { Id = "order-1", ItemCount = 0, TotalAmount = 0m };

        var updateCount = await _mapper.MapComplexAsync(source, target);

        target.ItemCount.Should().Be(10);
        target.TotalAmount.Should().Be(250m);
        updateCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MapPrimitiveAsync_WithFactory_ShouldNotApplyFactory()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 10, TotalAmount = 250m },
        };

        var target = new Order { Id = "order-1", ItemCount = 0, TotalAmount = 0m };

        await _mapper.MapPrimitiveAsync(source, target);

        target.ItemCount.Should().Be(0);
        target.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Factory_ShouldTrackChangesCorrectly()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 5, TotalAmount = 100m },
        };

        var target = new Order { Id = "order-1", ItemCount = 3, TotalAmount = 50m };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().BeGreaterThan(0);
        target.ItemCount.Should().Be(5);
        target.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public async Task Factory_WithNoChanges_ShouldReturnZeroCount()
    {
        var source = new OrderDto
        {
            Id = "order-1",
            Summary = new OrderSummaryDto { ItemCount = 5, TotalAmount = 100m },
        };

        var target = new Order { Id = "order-1", ItemCount = 5, TotalAmount = 100m };

        var updateCount = await _mapper.MapAsync(source, target);

        updateCount.Should().Be(0);
    }

    [Fact]
    public async Task MapAsync_WithMultiplePropertiesFromFactory_ShouldUpdateAll()
    {
        var source = new ProductDto
        {
            Id = "product-1",
            Name = "Widget",
            PriceInfo = new PriceInfoDto { BasePrice = 200m, Discount = 0.25m },
        };

        var target = new Product
        {
            Id = "product-1",
            Name = "Old",
            BasePrice = 0m,
            FinalPrice = 0m,
        };

        await _mapper.MapAsync(source, target);

        target.Name.Should().Be("Widget");
        target.BasePrice.Should().Be(200m);
        target.FinalPrice.Should().Be(150m);
    }

    [Fact]
    public void FactoryMappingContext_Add_ShouldAllowMethodChaining()
    {
        var services = new ServiceCollection();
        services.AddScoped<OrderSummaryFactory>();

        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(UpdatablePropertyMapperTests).Assembly);
        });

        var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<IRqlMapAccessor>();

        var entries = accessor.GetEntries(typeof(Order), typeof(OrderDto));

        entries.Should().NotBeEmpty();
        entries.Count(e => e.FactoryType != null).Should().Be(1);

        sp.Dispose();
    }

    [Fact]
    public void WithReadonly_RegistersReadOnlyFactoryOnMapEntry()
    {
        var services = new ServiceCollection();
        services.AddScoped<ProductReadonlyDisplayFactory>();
        services.AddRql(config =>
        {
            config.ScanForMappers(typeof(UpdatablePropertyMapperTests).Assembly);
        });

        var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<IRqlMapAccessor>();

        var entries = accessor.GetEntries(typeof(Product), typeof(ProductReadonlyDto));

        entries.Should().NotBeEmpty();
        entries.Count(e => e.FactoryType == typeof(ProductReadonlyDisplayFactory)).Should().Be(1);

        sp.Dispose();
    }

    public class Order
    {
        public string Id { get; set; } = null!;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderDto
    {
        public string Id { get; set; } = null!;
        public OrderSummaryDto? Summary { get; set; }
    }

    public class OrderSummaryDto
    {
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class Product
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
    }

    public class ProductDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public PriceInfoDto? PriceInfo { get; set; }
    }

    public class ProductReadonlyDto
    {
        public string Id { get; set; } = null!;
        public string Display { get; set; } = null!;
    }

    public class PriceInfoDto
    {
        public decimal BasePrice { get; set; }
        public decimal Discount { get; set; }
    }

    public class Customer
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }

    public class CustomerDto
    {
        public string Id { get; set; } = null!;
        public NameInfoDto? NameInfo { get; set; }
    }

    public class NameInfoDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }

    public class OrderSummaryFactory : UpdatablePropertyMapper<Order, OrderSummaryDto>
    {
        public override Expression<Func<Order, object?>> GetStorageExpression()
            => order => new { order.ItemCount, order.TotalAmount };

        public override void TryUpdate(OrderSummaryDto? input, Order entity, out bool hasChanges)
        {
            if (input is { } summary)
            {
                hasChanges = entity.ItemCount != summary.ItemCount || entity.TotalAmount != summary.TotalAmount;
                entity.ItemCount = summary.ItemCount;
                entity.TotalAmount = summary.TotalAmount;
            }
            else
            {
                hasChanges = entity.ItemCount != 0 || entity.TotalAmount != 0;
                entity.ItemCount = 0;
                entity.TotalAmount = 0;
            }
        }
    }

    public class ProductPriceCalculatorFactory : UpdatablePropertyMapper<Product, PriceInfoDto>
    {
        public override Expression<Func<Product, object?>> GetStorageExpression()
            => product => new { product.BasePrice, product.FinalPrice };

        public override void TryUpdate(PriceInfoDto? input, Product entity, out bool hasChanges)
        {
            if (input is { } priceInfo)
            {
                var calculatedPrice = priceInfo.BasePrice * (1 - priceInfo.Discount);
                hasChanges = entity.BasePrice != priceInfo.BasePrice || entity.FinalPrice != calculatedPrice;
                entity.BasePrice = priceInfo.BasePrice;
                entity.FinalPrice = calculatedPrice;
            }
            else
            {
                hasChanges = false;
            }
        }
    }

    public class CustomerFullNameFactory : UpdatablePropertyMapper<Customer, NameInfoDto>
    {
        public override Expression<Func<Customer, object?>> GetStorageExpression()
            => customer => customer.FullName;

        public override void TryUpdate(NameInfoDto? input, Customer entity, out bool hasChanges)
        {
            if (input is { } nameInfo)
            {
                var fullName = $"{nameInfo.FirstName} {nameInfo.LastName}";
                hasChanges = entity.FullName != fullName;
                entity.FullName = fullName;
            }
            else
            {
                hasChanges = false;
            }
        }
    }

    public class ProductReadonlyDisplayFactory : IRqlMappingExpressionFactory<Product>
    {
        public Expression<Func<Product, object?>> GetStorageExpression()
            => product => product.Name + " @ " + product.FinalPrice;

        public ExpressionFactoryHint Hint => ExpressionFactoryHint.None;
    }

    public class OrderToOrderDtoMap : IRqlMapper<Order, OrderDto>
    {
        public void MapEntity(IRqlMapperContext<Order, OrderDto> context)
        {
            context
                .MapDynamic(dest => dest.Id, src => src.Id)
                .MapComplex(t => t.Summary).With<OrderSummaryFactory>();
        }
    }

    public class ProductToProductDtoMap : IRqlMapper<Product, ProductDto>
    {
        public void MapEntity(IRqlMapperContext<Product, ProductDto> context)
        {
            context
                .MapDynamic(dest => dest.Id, src => src.Id)
                .MapDynamic(dest => dest.Name, src => src.Name)
                .MapComplex(t => t.PriceInfo).With<ProductPriceCalculatorFactory>();
        }
    }

    public class CustomerToCustomerDtoMap : IRqlMapper<Customer, CustomerDto>
    {
        public void MapEntity(IRqlMapperContext<Customer, CustomerDto> context)
        {
            context
                .MapDynamic(dest => dest.Id, src => src.Id)
                .MapComplex(src => src.NameInfo).With<CustomerFullNameFactory>();
        }
    }

    public class ProductToProductReadonlyDtoMap : IRqlMapper<Product, ProductReadonlyDto>
    {
        public void MapEntity(IRqlMapperContext<Product, ProductReadonlyDto> context)
        {
            context
                .MapDynamic(dest => dest.Id, src => src.Id)
                .MapComplex(t => t.Display).WithReadonly<ProductReadonlyDisplayFactory>();
        }
    }

    public class TestDynamicEntityMapper(IServiceProvider serviceProvider, IRqlMapAccessor rqlMapAccessor)
        : DynamicEntityMapper(serviceProvider, rqlMapAccessor)
    {
        public bool UseAssignForPlatformEntitiesOverride { get; set; }
        public bool EnsureCollectionLoadedAsyncResult { get; set; } = true;

        protected internal override bool UseAssignForPlatformEntities => UseAssignForPlatformEntitiesOverride;

        protected internal override Task<bool> EnsureCollectionLoadedAsync(object entity, PropertyInfo collectionProperty)
            => Task.FromResult(EnsureCollectionLoadedAsyncResult);

        protected internal override Task<object?> FindEntityAsync(Type entityType, object entity)
            => Task.FromResult<object?>(null);

        protected internal override Task<int> UpdatePlatformEntityReference(object declaringObject, PropertyInfo property, object? entity)
            => Task.FromResult(0);
    }
}
