using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork.Events;

public sealed class ChannelCreatedEvent : IEvent
{
    public Channel Channel { get; private set; }
    public IConnection Connection { get; private set; }
    public ChannelCreatedEvent(Channel channel, IConnection connection)
    {
        Channel = channel;
        Connection = connection;
    }
}
