using Microsoft.EntityFrameworkCore;
using Mpt.Framework.Persistence;
using Mpt.Rql;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence.Tests.Fixtures;

/// <summary>Per-entity query service that hands off to EF Core via the WidgetDbContext.</summary>
public class WidgetQueryService(
    IRqlQueryable<WidgetDbEntity, WidgetView> rql,
    IFilterProvider<WidgetDbEntity> filterProvider,
    IQueryExecutionStrategy executionStrategy,
    WidgetDbContext db)
    : QueryService<WidgetDbEntity, WidgetView>(rql, filterProvider)
{
    protected override IQueryExecutionStrategy QueryExecutionStrategy { get; } = executionStrategy;

    protected override string? AppendOrderProperty => "Id";

    protected override IQueryable<WidgetDbEntity> GetQuery() => db.Widgets;

    protected override Expression<Func<WidgetDbEntity, bool>> GetByIdPredicate(object byId)
    {
        var id = byId.ToString();
        return w => w.Id == id;
    }
}

/// <summary>RQL mapper for the widget entity.</summary>
public class WidgetMap : IRqlMapper<WidgetDbEntity, WidgetView>
{
    public void MapEntity(IRqlMapperContext<WidgetDbEntity, WidgetView> context)
    {
        context.MapStatic(v => v.Id, d => d.Id);
        context.MapStatic(v => v.Revision, d => d.Revision);
        context.MapStatic(v => v.Name, d => d.Name);
        context.MapStatic(v => v.Count, d => d.Count);
    }
}
