using Mpt.Rql;
using System.Linq.Expressions;

namespace Mpt.Framework.Mapping;

/// <summary>
/// Fluent intermediate returned by <see cref="RqlMappingExtensions.MapComplex{TSource,TView,TProperty}"/>
/// that binds a target property to a custom factory.
/// </summary>
public class ComplexMappingContext<TStorage, TView, TProperty>(
    Expression<Func<TView, TProperty?>> targetProperty,
    IRqlMapperContext<TStorage, TView> context)
{
    private readonly Expression<Func<TView, object?>> _objectExpression = Expression.Lambda<Func<TView, object?>>(
        Expression.Convert(targetProperty.Body, typeof(object)),
        targetProperty.Parameters);

    private readonly IRqlMapperContext<TStorage, TView> _context = context;

    /// <summary>
    /// Wires the target property to an <see cref="UpdatablePropertyMapper{TStorage,TProperty}"/>
    /// that drives both query projection and the dynamic mapper's update pass.
    /// </summary>
    public void With<TFactory>() where TFactory : UpdatablePropertyMapper<TStorage, TProperty>
        => _context.MapWithFactory<TFactory>(_objectExpression);

    /// <summary>
    /// Wires the target property to a read-only factory that supplies a projection
    /// expression but does not participate in updates.
    /// </summary>
    public void WithReadonly<TFactory>() where TFactory : class, IRqlMappingExpressionFactory<TStorage>
        => _context.MapWithFactory<TFactory>(_objectExpression);
}
