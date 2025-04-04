using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork.Events;

public sealed class DisposedEvent : IEvent
{
    public IConnection Connection { get; private set; }
    public DisposedEvent(IConnection connection)
    {
        Connection = connection;
    }
}
