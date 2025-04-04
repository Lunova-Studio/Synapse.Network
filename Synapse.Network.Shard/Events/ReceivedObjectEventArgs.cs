using Synapse.Network.Shard.Interfaces;

namespace Synapse.Network.Shard.Events;

public sealed class ReceivedObjectEventArgs(IChannel channel, IConnection connection, object value) : EventArgs, IEvent {
    public object Value => value;
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ReceivedObjectEventArgs).Name;
}