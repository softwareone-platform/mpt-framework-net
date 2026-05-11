namespace Mpt.Framework.MessageHub;

public class InputStreamFilter
{
    /// <summary>
    /// When true the stream will consume events originating from its own module even when
    /// no explicit module filter is set. Default: false.
    /// </summary>
    public bool AllowOwnEvents { get; set; }

    public string[]? Modules { get; set; }

    public string[]? Entities { get; set; }

    public string[]? Events { get; set; }
}
