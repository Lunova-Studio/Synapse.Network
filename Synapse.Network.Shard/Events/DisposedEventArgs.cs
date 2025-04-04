using Synapse.Network.Shard.Interfaces;

namespace Synapse.Network.Shard.Events;
public sealed class DisposedEventArgs(IConnection connection) : EventArgs, IEvent {
    public IConnection Connection => connection;
    public string EventName => typeof(DisposedEventArgs).Name;
}