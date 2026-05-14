using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests;

public class QueryServiceBranchTests
{
    [Fact]
    public async Task ListAsync_WithOrderBy_AppliesAscendingOrder()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w3", Name = "x", Count = 3 },
            new WidgetDbEntity { Id = "w1", Name = "x", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "x", Count = 2 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListAsync(w => w.Name == "x", cfg => cfg.OrderBy(w => w.Count!), CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Select(r => r.Count).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ListAsync_WithOrderByDescending_AppliesDescendingOrder()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "x", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "x", Count = 2 },
            new WidgetDbEntity { Id = "w3", Name = "x", Count = 3 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListAsync(w => w.Name == "x", cfg => cfg.OrderByDescending(w => w.Count!), CancellationToken.None);

        rows.Should().HaveCount(3);
        rows.Select(r => r.Count).Should().Equal(3, 2, 1);
    }

    [Fact]
    public async Task ListAsync_WithChainedOrder_AppliesThenBy()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "b", Count = 2 },
            new WidgetDbEntity { Id = "w2", Name = "a", Count = 2 },
            new WidgetDbEntity { Id = "w3", Name = "a", Count = 1 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListAsync(
            w => true,
            cfg => cfg.OrderBy(w => w.Name!).ThenByDescending(w => w.Count!),
            CancellationToken.None);

        rows.Select(r => r.Id).Should().Equal("w2", "w3", "w1");
    }

    [Fact]
    public async Task GetPageAsync_WithRqlOrder_AppliesAndIncludesAppendOrderTieBreaker()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w2", Name = "a", Count = 1 },
            new WidgetDbEntity { Id = "w1", Name = "a", Count = 1 });
        await db.SaveChangesAsync();

        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<WidgetView>>();
        var page = await queryService.GetPageAsync(
            new DataPageRequest(new RqlRequest { Order = "name" }, 10, 0, false),
            CancellationToken.None);

        page.Data.Should().HaveCount(2);
        // Append-order tie-breaker on Id (ascending) puts w1 before w2 when name ties.
        page.Data.Select(r => r.Id).Should().Equal("w1", "w2");
    }

    [Fact]
    public async Task GetPageAsync_WithInvalidRqlFilter_ThrowsInvalidOperationException()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<WidgetView>>();

        var act = async () => await queryService.GetPageAsync(
            new DataPageRequest(new RqlRequest { Filter = "not-a-valid-rql(((" }, 10, 0, false),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
