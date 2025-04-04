using Synapse.Network.Shard.Interfaces;

namespace Synapse.Network.Shard.Events;

public sealed class ChannelCreatedEventArgs(IChannel channel, IConnection connection) : EventArgs, IEvent {
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ChannelCreatedEventArgs).Name;
}