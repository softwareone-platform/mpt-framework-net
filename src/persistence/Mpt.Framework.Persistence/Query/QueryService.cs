using FluentValidation;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Abstract base for <see cref="IQueryService{TEntity}"/> implementations. Wires the RQL
/// pipeline (filter, order, project, paginate) plus custom-filter extensibility, and
/// delegates execution to an <see cref="IQueryExecutionStrategy"/>. Subclass for each
/// (TDbEntity, TEntity) pair and override <see cref="GetQuery"/> /
/// <see cref="GetByIdPredicate"/> at minimum.
/// </summary>
/// <typeparam name="TDbEntity">The persistence-side entity type.</typeparam>
/// <typeparam name="TEntity">The projected view-model.</typeparam>
public abstract class QueryService<TDbEntity, TEntity>(
    IRqlQueryable<TDbEntity, TEntity> rql,
    IFilterProvider<TDbEntity> customFilterProvider)
    : IQueryService<TEntity>
    where TDbEntity : class
    where TEntity : class, IRqlGraphHolder
{
    /// <inheritdoc />
    public async Task<object?> GetAsObjectAsync(object id, RqlRequest request, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => await GetAsync(id, request, static t => t, cancellationToken, configure);

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetAsync(object id, RqlRequest request, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => await GetOneAsync(request, queryCallback, GetByIdPredicate(id), false, configure, cancellationToken);

    /// <inheritdoc />
    public async Task<TEntity?> GetAsync(RqlRequest request, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
        => await GetOneAsync(request, queryCallback, null, true, configure, cancellationToken);

    private async Task<TEntity?> GetOneAsync(
        RqlRequest request,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback,
        Expression<Func<TDbEntity, bool>>? predicate,
        bool assertUniqueness,
        Action<IRqlSettings>? configure,
        CancellationToken cancellationToken)
    {
        var page = await ExecutePageRequestAsync(
            new DataPageRequest(request, assertUniqueness ? 2 : 1, 0, false),
            predicate == null ? BuildQuery() : BuildQuery().Where(predicate),
            queryCallback,
            settings =>
            {
                RqlDefaults.SetSingleItemDefaults(settings);
                OnItemRequestReady(request, settings);
                configure?.Invoke(settings);
            },
            cancellationToken);

        var item = assertUniqueness ? page.Data.SingleOrDefault() : page.Data.FirstOrDefault();

        if (item != null)
        {
            ((IRqlGraphHolder)item).RqlGraph = page.RqlGraph;
        }

        return item;
    }

    /// <inheritdoc />
    public async Task<TResult?> GetShapedAsync<TResult>(string id, RqlRequest request, Expression<Func<TEntity, TResult>> shaper, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
    {
        var result = TransformAndHandleErrors(BuildQuery().Where(GetByIdPredicate(id)), request, settings =>
        {
            RqlDefaults.SetSingleItemDefaults(settings);
            OnItemRequestReady(request, settings);
            configure?.Invoke(settings);
        });
        var query = result.Query.Select(shaper);
        return await QueryExecutionStrategy.FirstOrDefaultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<DataPage<TEntity>> GetPageAsync(DataPageRequest pageRequest,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback, CancellationToken cancellationToken, Action<IRqlSettings>? configure = null)
    {
        var query = customFilterProvider.Apply(pageRequest.CustomFilters, BuildQuery());

        // Append a stable order key when the caller supplied an order clause but no tie-breaker —
        // EF Core's split-query mode otherwise risks dropping rows.
        if (!string.IsNullOrEmpty(pageRequest.Request.Order) && !string.IsNullOrEmpty(AppendOrderProperty))
        {
            pageRequest.Request.Order = $"{pageRequest.Request.Order.TrimStart('(').TrimEnd(')')},{AppendOrderProperty}";
        }

        return ExecutePageRequestAsync(pageRequest, query, queryCallback, settings =>
        {
            RqlDefaults.SetListDefaults(settings);
            OnPageRequestReady(pageRequest.Request, settings);
            configure?.Invoke(settings);
        }, cancellationToken);
    }

    /// <summary>Hook invoked just before single-entity reads to tune the resolved RQL settings.</summary>
    protected virtual void OnItemRequestReady(RqlRequest rqlRequest, IRqlSettings settings) { }

    /// <summary>Hook invoked just before list reads to tune the resolved RQL settings.</summary>
    protected virtual void OnPageRequestReady(RqlRequest rqlRequest, IRqlSettings settings) { }

    /// <summary>The async execution strategy backing this query service.</summary>
    protected abstract IQueryExecutionStrategy QueryExecutionStrategy { get; }

    /// <summary>
    /// Optional tie-breaker property appended to the order clause when the caller supplies
    /// one but no unique sort key. Return <see langword="null"/> to suppress.
    /// </summary>
    protected abstract string? AppendOrderProperty { get; }

    /// <summary>Returns the raw queryable over the persistence-side entity.</summary>
    protected IQueryable<TDbEntity> BuildQuery() => ApplyContext(GetQuery());

    /// <summary>The data source. EF Core implementations return <c>DbContext.Set&lt;TDbEntity&gt;()</c>.</summary>
    protected abstract IQueryable<TDbEntity> GetQuery();

    /// <summary>
    /// Optional query transformation applied before every read. Override to inject
    /// tenant filters or other ambient conditions. Default-impl is a passthrough.
    /// </summary>
    protected virtual IQueryable<TDbEntity> ApplyContext(IQueryable<TDbEntity> query) => query;

    /// <summary>The predicate that selects the single entity matching the supplied id.</summary>
    protected abstract Expression<Func<TDbEntity, bool>> GetByIdPredicate(object byId);

    /// <summary>Hook invoked once the query has been transformed and is about to execute.</summary>
    protected virtual void OnQueryCreated(IQueryable<TEntity> query) { }

    private async Task<DataPage<TEntity>> ExecutePageRequestAsync(
        DataPageRequest pageRequest,
        IQueryable<TDbEntity> source,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryCallback,
        Action<IRqlSettings> configure,
        CancellationToken cancellationToken)
    {
        var queryResult = TransformAndHandleErrors(source, pageRequest.Request, configure);

        var query = queryCallback(queryResult.Query);

        OnQueryCreated(query);

        var data = new DataPage<TEntity>
        {
            RqlGraph = queryResult.Graph,
        };

        if (pageRequest.Limit == 0)
        {
            data.Data = [];
        }
        else if (pageRequest.Offset == 0 && pageRequest.Limit == 1)
        {
            var singleItem = await QueryExecutionStrategy.FirstOrDefaultAsync(query, cancellationToken);
            data.Data = singleItem != null ? [singleItem] : [];
        }
        else
        {
            data.Data = await QueryExecutionStrategy.ToListAsync(query.Skip(pageRequest.Offset).Take(pageRequest.Limit), cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (pageRequest.CountAll)
        {
            data.Total = await QueryExecutionStrategy.CountAsync(query, cancellationToken);
        }

        return data;
    }

    private RqlResponse<TEntity> TransformAndHandleErrors(IQueryable<TDbEntity> source, RqlRequest rqlRequest, Action<IRqlSettings> configure)
    {
        RqlResponse<TEntity> queryResult;
        try
        {
            queryResult = rql.Transform(source, rqlRequest, configure);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"RQL transform failed: {e.Message}", e);
        }

        if (!queryResult.IsSuccess)
        {
            ThrowRqlException(queryResult.Errors);
        }

        return queryResult;
    }

    private static void ThrowRqlException(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException("Unknown RQL error");
        }

        if (errors.TrueForAll(error => error.Type == ErrorType.Validation))
        {
            throw new ValidationException(errors.Select(s => new FluentValidation.Results.ValidationFailure(s.Path ?? s.Code, s.Message)));
        }

        throw new InvalidOperationException(errors[0].Message);
    }
}
