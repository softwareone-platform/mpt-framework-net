namespace Mpt.Framework.MessageHub;

/// <summary>
/// Vehicle for events that do not fit the standard CRUD categories. Produced by
/// <see cref="IEntityEventProducer{TEntity}.ProduceCustomEvents"/> after a caller has
/// registered the event's descriptor via
/// <see cref="IEntityEventProducer{TEntity}.RegisterCustomEvent"/>. The descriptor must
/// supply <see cref="IEventDescriptor.EventKey"/>, <see cref="IEventDescriptor.Summary"/>,
/// and <see cref="IEventDescriptor.Description"/>; otherwise the producer throws.
/// </summary>
/// <remarks>
/// Public (rather than internal as upstream) so the engine-package
/// <c>EntityEventProducer&lt;TEntity&gt;</c> can construct instances from a different
/// assembly.
/// </remarks>
public class CustomEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity, new()
{
    private const string Unconfigured = "unconfigured_event";

    /// <summary>Constructs the custom event. The descriptor supplied later by <c>Customize</c> drives its routing key, summary, and description.</summary>
    public CustomEvent(string module, TEntity entity, TEntity? original, PlatformEventPermissionsBuilder permissionsBuilder)
        : base(module, entity, original, permissionsBuilder)
    {
    }

    /// <inheritdoc/>
    public override string EventKey => Unconfigured;

    /// <inheritdoc/>
    protected override string GetSummary(string entityName) => Unconfigured;

    /// <inheritdoc/>
    protected override string GetDescription(string entityName, string entityKey) => Unconfigured;
}
