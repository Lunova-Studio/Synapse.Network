using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Shared.Events;

public sealed class ChannelCreatedEventArgs(IChannel channel, IConnection connection) : EventArgs, IEvent {
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ChannelCreatedEventArgs).Name;
}