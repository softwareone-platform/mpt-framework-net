namespace Mpt.Framework.MessageHub;

[Flags]
public enum StreamTypes
{
    None = 0,
    Events = 1 << 0,
    Sync = 1 << 1,
    System = 1 << 2
}
