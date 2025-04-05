using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Shared.Events;

public sealed class ChannelDeletedEventArgs(IChannel channel, IConnection connection) : EventArgs, IEvent {
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ChannelDeletedEventArgs).Name;
}