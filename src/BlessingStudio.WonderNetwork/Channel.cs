using BlessingStudio.WonderNetwork.Events;
using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork;

public class Channel
{
    public IConnection Connection { get; private set; }
    public string ChannelName { get; private set; }
    public static bool operator ==(Channel left, Channel right)
    {
        return left.Connection == right.Connection && left.ChannelName == right.ChannelName;
    }
    public static bool operator !=(Channel left, Channel right)
    {
        return !(left == right);
    }
    public override bool Equals(object? obj)
    {
        if (obj is Channel other)
        {
            return this == other;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return Connection.GetHashCode() + ChannelName.GetHashCode();
    }
    public Channel(IConnection connection, string channelName)
    {
        Connection = connection;
        ChannelName = channelName;
    }
    public void Send(byte[] data)
    {
        Connection.Send(ChannelName, data);
    }
    public void Send<T>(T @object)
    {
        Connection.Send(ChannelName, @object);
    }
    public void AddHandler(Events.EventHandler<ReceivedBytesEvent> handler)
    {
        Connection.AddHandler((ReceivedBytesEvent e) =>
        {
            if (e.Channel == this)
            {
                handler(e);
            }
        });
    }
    public void AddHandler(Events.EventHandler<ReceivedObjectEvent> handler)
    {
        Connection.AddHandler((ReceivedObjectEvent e) =>
        {
            if (e.Channel == this)
            {
                handler(e);
            }
        });
    }
    public T? WaitFor<T>(TimeSpan timeout)
    {
        return Connection.WaitFor<T>(ChannelName, timeout);
    }
    public T? WaitFor<T>(CancellationToken cancellationToken = default)
    {
        return Connection.WaitFor<T>(ChannelName, cancellationToken);
    }
}
