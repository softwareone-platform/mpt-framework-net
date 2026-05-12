using System.Diagnostics;

namespace Mpt.Framework.MessageHub;

internal readonly record struct TracedTransport<T>(T Message, ActivityContext? ActivityContext);
