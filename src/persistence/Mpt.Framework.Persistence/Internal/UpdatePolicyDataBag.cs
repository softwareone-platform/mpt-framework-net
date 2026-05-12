using Mpt.Framework.Delta;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Concrete runtime instance of <see cref="IUpdatePolicyData{TType, TProperty}"/> with
/// a typed <typeparamref name="TProperty"/>.
/// </summary>
internal class UpdatePolicyData<TEntity, TProperty> : IUpdatePolicyData<TEntity, TProperty>
{
    public TEntity Entity { get; internal set; } = default!;
    public Delta<TEntity> Delta { get; internal set; } = null!;
    public IReadOnlyCollection<string> Roles { get; internal set; } = Array.Empty<string>();
    public string Action { get; internal set; } = null!;
    public bool IsDefined { get; internal set; }
    public TProperty? Original { get; internal set; }
    public TProperty? Updated { get; internal set; }
}

/// <summary>
/// Untyped-property data bag used by the engine before the property type is known.
/// Converts to a typed view lazily through <see cref="ToTarget{TTarget}"/>.
/// </summary>
internal class UpdatePolicyDataBag<TEntity> : UpdatePolicyData<TEntity, object>
{
    private object? _converted;

    public UpdatePolicyData<TEntity, TTarget> ToTarget<TTarget>()
    {
        _converted ??= new UpdatePolicyData<TEntity, TTarget>
        {
            Entity = Entity,
            Delta = Delta,
            Roles = Roles,
            Action = Action,
            IsDefined = IsDefined,
            Original = Original is TTarget original ? original : default,
            Updated = Updated is TTarget updated ? updated : default,
        };

        return (UpdatePolicyData<TEntity, TTarget>)_converted;
    }
}
