namespace Mpt.Framework.MessageHub;

public interface IPlatformEvent
{
    EventMessage MakeMessage();
}
