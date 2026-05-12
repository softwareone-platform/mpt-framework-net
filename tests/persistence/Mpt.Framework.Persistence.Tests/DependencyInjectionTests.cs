using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.EFCore;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void Build_ResolvesRepositoryUnitOfWorkAndQueryService()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<WidgetView>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<WidgetView>>();

        repo.Should().BeOfType<EfCoreRepository<WidgetDbEntity, WidgetView>>();
        unitOfWork.Should().BeOfType<EfCoreUnitOfWork>();
        queryService.Should().BeOfType<WidgetQueryService>();
    }

    [Fact]
    public void Build_ResolvesDefaultEntityConfigurationAndHooks()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEntityConfiguration<WidgetView>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IEntityLifecycleHooks<WidgetView>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IEntityEventProducer<WidgetView>>().Should().NotBeNull();
    }

    [Fact]
    public void UnitOfWork_GetRepository_CachesPerType()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var first = unitOfWork.GetRepository<WidgetView>();
        var second = unitOfWork.GetRepository<WidgetView>();

        first.Should().BeSameAs(second);
    }
}
