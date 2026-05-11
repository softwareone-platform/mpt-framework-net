namespace Mpt.Framework.MessageHub;

/// <summary>
/// Message header names set by the publisher and read by Azure Service Bus SQL rule
/// filters on the consuming side. Exposed publicly so transport-level tools (e.g.
/// migration / replay utilities) can inspect or filter on the same fields.
/// </summary>
public static class MessageHubHeaders
{
    public const string StreamType = "mpt_stream_type";
    public const string SourceModule = "mpt_source_module";
    public const string TargetModules = "mpt_target_modules";
    public const string Entity = "mpt_entity";
    public const string Event = "mpt_event";
}
