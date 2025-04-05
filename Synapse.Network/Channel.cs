using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network;

public sealed class Channel : IChannel {
    public string ChannelName { get; }
    public IConnection Connection { get; }

    public Channel(IConnection connection, string channelName) {
        Connection = connection;
        ChannelName = channelName;
    }

    public Task SendAsync<T>(T obj) => Connection.SendAsync(ChannelName, obj);
    public Task SendAsync(Memory<byte> data) => Connection.SendAsync(ChannelName, data);

    public override int GetHashCode() {
        return Connection.GetHashCode() + ChannelName.GetHashCode();
    }

    public override bool Equals(object? obj) {
        if (obj is Channel c)
            return this == c;

        return false;
    }

    public static bool operator ==(Channel c1, Channel c2) {
        return c1.Connection == c2.Connection && c1.ChannelName == c2.ChannelName;
    }

    public static bool operator !=(Channel c1, Channel c2) {
        return !(c1 == c2);
    }
}