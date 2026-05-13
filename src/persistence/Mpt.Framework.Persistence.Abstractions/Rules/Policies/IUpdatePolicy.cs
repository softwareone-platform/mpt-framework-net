using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Top-level update-policy DSL — anchor a configuration scope on a specific entity
/// property or collection property and refine the rules via <see cref="IUpdatePolicyProperty{TRoot, TProperty}"/>.
/// </summary>
public interface IUpdatePolicy<TEntity>
{
    /// <summary>Configure update rules for a single property.</summary>
    IUpdatePolicy<TEntity> Property<TProperty>(Expression<Func<TEntity, TProperty>> property, Action<IUpdatePolicyProperty<TEntity, TProperty>> configure);

    /// <summary>Configure update rules for a collection property, applied per-element.</summary>
    IUpdatePolicy<TEntity> Collection<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> property, Action<IUpdatePolicyProperty<TEntity, TProperty>> configure);
}

/// <summary>
/// Convenience extensions for the common Allow/Forbid/Require/Ignore-property cases
/// that don't need to nest into the property's children.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin extension shims that forward to UpdatePolicyBuilders — the underlying builder is exercised by the persistence integration tests.")]
public static class UpdatePolicyExtensions
{
    /// <summary>Allow the named property to be set.</summary>
    public static IUpdatePolicy<TEntity> Allow<TEntity, TProperty>(this IUpdatePolicy<TEntity> policy, Expression<Func<TEntity, TProperty>> property)
        => policy.Property(property, static p => p.Allow());

    /// <summary>Forbid the named property from being set.</summary>
    public static IUpdatePolicy<TEntity> Forbid<TEntity, TProperty>(this IUpdatePolicy<TEntity> policy, Expression<Func<TEntity, TProperty>> property)
        => policy.Property(property, static p => p.Forbid());

    /// <summary>Require the named property to be set.</summary>
    public static IUpdatePolicy<TEntity> Require<TEntity, TProperty>(this IUpdatePolicy<TEntity> policy, Expression<Func<TEntity, TProperty>> property)
        => policy.Property(property, static p => p.Require());

    /// <summary>Silently ignore the named property.</summary>
    public static IUpdatePolicy<TEntity> Ignore<TEntity, TProperty>(this IUpdatePolicy<TEntity> policy, Expression<Func<TEntity, TProperty>> property)
        => policy.Property(property, static p => p.Ignore());
}

/// <summary>
/// Convenience extensions for <see cref="IUpdatePolicyProperty{TRoot, TProperty}"/> that
/// pair Allow/Forbid/Require with an action filter or with a single nested property.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin extension shims that forward to UpdatePolicyBuilders — the underlying builder is exercised by the persistence integration tests.")]
public static class UpdatePolicyPropertyExtensions
{
    /// <summary>Allow a nested property to be set.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TSubProperty> Allow<TEntity, TProperty, TSubProperty>(this IUpdatePolicyProperty<TEntity, TProperty> policy, Expression<Func<TProperty, TSubProperty>> property)
        => policy.Property(property).Allow();

    /// <summary>Forbid a nested property from being set.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TSubProperty> Forbid<TEntity, TProperty, TSubProperty>(this IUpdatePolicyProperty<TEntity, TProperty> policy, Expression<Func<TProperty, TSubProperty>> property)
        => policy.Property(property).Forbid();

    /// <summary>Require a nested property to be set.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TSubProperty> Require<TEntity, TProperty, TSubProperty>(this IUpdatePolicyProperty<TEntity, TProperty> policy, Expression<Func<TProperty, TSubProperty>> property)
        => policy.Property(property).Require();

    /// <summary>Require the property to be set, restricted to the supplied action.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TProperty> RequireOn<TEntity, TProperty>(this IUpdatePolicyProperty<TEntity, TProperty> property, EntityAction action)
        => property.RequireOn(action.ToString());

    /// <summary>Require the property to be set, restricted to the named action.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TProperty> RequireOn<TEntity, TProperty>(this IUpdatePolicyProperty<TEntity, TProperty> property, string action)
        => property.Require().On(action);

    /// <summary>Allow the property to be set, restricted to the supplied action.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TProperty> AllowOn<TEntity, TProperty>(this IUpdatePolicyProperty<TEntity, TProperty> property, EntityAction action)
        => property.AllowOn(action.ToString());

    /// <summary>Allow the property to be set, restricted to the named action.</summary>
    public static IUpdatePolicyRuleBuilder<TEntity, TProperty> AllowOn<TEntity, TProperty>(this IUpdatePolicyProperty<TEntity, TProperty> property, string action)
        => property.Allow().On(action);
}
