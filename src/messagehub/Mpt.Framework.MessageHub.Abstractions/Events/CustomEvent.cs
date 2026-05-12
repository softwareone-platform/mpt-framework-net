namespace Mpt.Framework.MessageHub;

/// <summary>
/// Vehicle for events that do not fit the standard CRUD categories. The descriptor
/// supplied via <see cref="PlatformEvent.Customize(Action{IEventDescriptor})"/> must
/// set <see cref="IEventDescriptor.EventKey"/>, <see cref="IEventDescriptor.Summary"/>,
/// and <see cref="IEventDescriptor.Description"/>.
/// </summary>
public class CustomEvent<TEntity> : GenericEvent<TEntity>
    where TEntity : class, IPlatformEntity, new()
{
    private const string Unconfigured = "unconfigured_event";

    public CustomEvent(string module, TEntity entity, TEntity? original, PlatformEventPermissionsBuilder permissionsBuilder)
        : base(module, entity, original, permissionsBuilder)
    {
    }

    public override string EventKey => Unconfigured;

    protected override string GetSummary(string entityName) => Unconfigured;

    protected override string GetDescription(string entityName, string entityKey) => Unconfigured;
}
