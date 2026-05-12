using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests;

public class QueryPagingTests
{
    [Fact]
    public async Task GetPageAsync_returns_paged_results_with_correct_Total_when_CountAll_is_set()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        for (var i = 1; i <= 5; i++)
        {
            db.Widgets.Add(new WidgetDbEntity { Id = $"w{i}", Name = $"name-{i}", Count = i });
        }
        await db.SaveChangesAsync();

        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<WidgetView>>();
        var page = await queryService.GetPageAsync(new DataPageRequest(new RqlRequest(), 2, 1, true), CancellationToken.None);

        page.Total.Should().Be(5);
        page.Data.Should().HaveCount(2);
        page.RqlGraph.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_by_id_through_the_query_service_returns_the_view()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "only", Count = 42 });
        await db.SaveChangesAsync();

        var queryService = scope.ServiceProvider.GetRequiredService<IQueryService<WidgetView>>();
        var widget = await queryService.GetAsync("w1", CancellationToken.None);

        widget.Should().NotBeNull();
        widget!.Name.Should().Be("only");
        widget.Count.Should().Be(42);
        widget.RqlGraph.Should().NotBeNull("query service stamps the projection graph on the returned view");
    }
}
