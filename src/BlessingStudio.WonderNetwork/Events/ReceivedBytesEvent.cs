using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork.Events;

public sealed class ReceivedBytesEvent : IEvent
{
    public Channel Channel { get; private set; }
    public IConnection Connection { get; private set; }
    public byte[] Data { get; private set; }
    public ReceivedBytesEvent(Channel channel, IConnection connection, byte[] data)
    {
        Channel = channel;
        Connection = connection;
        Data = data;
    }
}
