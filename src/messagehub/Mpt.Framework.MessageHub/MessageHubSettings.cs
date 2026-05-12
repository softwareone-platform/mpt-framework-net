using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.MessageHub;

public class MessageHubSettings
{
    public string? ConnectionString { get; set; }

    public MessageHubCleanupMode CleanupMode { get; set; } = MessageHubCleanupMode.None;

    public MessageHubTransport Transport { get; set; } = MessageHubTransport.ServiceBus;

    /// <summary>
    /// Name of the outbound topic / queue. Publishers send to this; consumers create
    /// subscriptions on it. Default: <c>marketplace.platform.messages</c>.
    /// </summary>
    public string OutputStream { get; set; } = "marketplace.platform.messages";

    /// <summary>
    /// How the <see cref="IPlatformEventEmitter"/> hands <see cref="EventMessage"/> instances
    /// to the leaf <see cref="IMessageHubPublisher"/>. Default <see cref="MessageHubPublishMode.Immediate"/>
    /// — caller awaits each send inline. Switch to <see cref="MessageHubPublishMode.Background"/>
    /// to queue messages into an in-process channel drained by a hosted service (preserves
    /// request throughput at the cost of a slightly delayed publish).
    /// </summary>
    public MessageHubPublishMode PublishMode { get; set; } = MessageHubPublishMode.Immediate;
}

public enum MessageHubCleanupMode
{
    None,
    DeleteEmptyUnknown,
    DeleteAnyUnknown,
}

public enum MessageHubTransport
{
    InMemory,
    ServiceBus
}

public enum MessageHubPublishMode
{
    /// <summary>Publish events synchronously — the caller awaits the MassTransit send.</summary>
    Immediate,

    /// <summary>Buffer events to an in-process channel drained by a background hosted service.</summary>
    Background,
}

public class MessageHubBuilder
{
    internal MessageHubBuilder(IServiceCollection services, string moduleCode)
        : this(services, moduleCode, null) { }

    internal MessageHubBuilder(IServiceCollection services, string moduleCode, MessageHubSettings? settings)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be null or empty", nameof(moduleCode));

        Services = services;
        ModuleCode = moduleCode;
        Settings = settings ?? new MessageHubSettings();
    }

    public IServiceCollection Services { get; }

    public string ModuleCode { get; }

    public MessageHubSettings Settings { get; }

    /// <summary>
    /// Optional hook invoked once for every <see cref="EventMessage"/> just before it is
    /// published. Useful for tagging, telemetry, or per-test inspection.
    /// </summary>
    public Action<EventMessage>? OnMessagePublishing { get; set; }

    public MessageHubBuilder ConfigureInput<TStreamProvider>()
        where TStreamProvider : IStreamProvider, new()
        => ConfigureInput(new TStreamProvider());

    public MessageHubBuilder ConfigureInput<TStreamProvider>(TStreamProvider instance)
        where TStreamProvider : IStreamProvider
    {
        StreamProviders.Add(instance);
        return this;
    }

    internal List<IStreamProvider> StreamProviders { get; } = [];
}
