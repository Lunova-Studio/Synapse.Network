using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork.Events;

public sealed class ChannelDeletedEvent : IEvent
{
    public string Channel { get; private set; }
    public IConnection Connection { get; private set; }
    public ChannelDeletedEvent(string channel, IConnection connection)
    {
        Channel = channel;
        Connection = connection;
    }
}
