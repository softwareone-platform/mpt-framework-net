using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence.Internal;

internal class GetEntityOptions : IGetEntityOptions
{
    private Action<IRqlSettings>? _configureAction;

    public RqlRequest Request { get; set; } = new();

    public void Configure(Action<IRqlSettings> configure) => _configureAction = configure;

    internal void ApplyConfiguration(IRqlSettings settings) => _configureAction?.Invoke(settings);
}

internal class GetEntityListOptions<TEntity> : GetEntityOptions, IGetEntityListOptions<TEntity>
{
    public ListOrderOptions<TEntity>? Order { get; private set; }

    public int Offset { get; set; }

    public int Limit { get; set; } = int.MaxValue;

    public IListOrderOptions<TEntity> OrderBy(Expression<Func<TEntity, object>> property)
    {
        Order = new ListOrderOptions<TEntity>(property, 1);
        return Order;
    }

    public IListOrderOptions<TEntity> OrderByDescending(Expression<Func<TEntity, object>> property)
    {
        Order = new ListOrderOptions<TEntity>(property, -1);
        return Order;
    }
}

internal class ListOrderOptions<TEntity>(Expression<Func<TEntity, object>> property, int direction) : IListOrderOptions<TEntity>
{
    private readonly List<(Expression<Func<TEntity, object>>, int)> _orders = [new(property, direction)];

    public void ThenBy(Expression<Func<TEntity, object>> property) => _orders.Add(new(property, 1));

    public void ThenByDescending(Expression<Func<TEntity, object>> property) => _orders.Add(new(property, -1));

    public IEnumerable<(Expression<Func<TEntity, object>> Property, int Direction)> Enumerate() => _orders;
}
