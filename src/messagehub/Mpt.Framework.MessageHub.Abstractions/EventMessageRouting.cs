namespace Mpt.Framework.MessageHub;

public class EventMessageRouting
{
    public StreamTypes Stream { get; set; }

    public string SourceModule { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string Event { get; set; } = null!;

    public List<string> TargetModules { get; set; } = [];

    public TimeSpan? Delay { get; set; }

    public string ToPath() => $"platform.{SourceModule.ToEventPathString()}.{Entity.ToEventPathString()}.{Event}";
}
