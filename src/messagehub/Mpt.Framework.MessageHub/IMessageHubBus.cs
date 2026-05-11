using MassTransit;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// The MassTransit bus dedicated to MessageHub traffic. Resolve this when you need the
/// underlying bus (e.g. for explicit endpoint sends or test control); prefer
/// <see cref="IMessageHubPublisher"/> for normal publish operations.
/// </summary>
public interface IMessageHubBus : IBus;
