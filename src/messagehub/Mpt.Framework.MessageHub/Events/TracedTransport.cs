using System.Diagnostics;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// Pairs an in-flight message with the <see cref="ActivityContext"/> that produced it so
/// the background publisher can reconstruct the trace span when it eventually sends the
/// message. Captured at <see cref="IPlatformEventEmitter.Register(IPlatformEvent)"/> time.
/// </summary>
internal readonly record struct TracedTransport<T>(T Message, ActivityContext? ActivityContext);
