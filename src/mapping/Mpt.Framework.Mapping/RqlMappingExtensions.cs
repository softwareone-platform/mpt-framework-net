using Mpt.Framework.Mapping;
using System.Linq.Expressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace

namespace Mpt.Rql;

/// <summary>
/// Extensions on <see cref="IRqlMapperContext{TStorage,TView}"/> that route complex property
/// mappings to custom factories.
/// </summary>
public static class RqlMappingExtensions
{
    /// <summary>
    /// Starts a fluent registration for a complex property mapping. Chain with
    /// <see cref="ComplexMappingContext{TStorage,TView,TProperty}.With{TFactory}"/> or
    /// <see cref="ComplexMappingContext{TStorage,TView,TProperty}.WithReadonly{TFactory}"/>
    /// to wire a factory.
    /// </summary>
    public static ComplexMappingContext<TStorage, TView, TProperty> MapComplex<TStorage, TView, TProperty>(
        this IRqlMapperContext<TStorage, TView> context,
        Expression<Func<TView, TProperty?>> propertySelector)
        => new(propertySelector, context);
}
