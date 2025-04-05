using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Shared.Events;

public sealed class ReceivedObjectEventArgs(IChannel channel, IConnection connection, object value) : EventArgs, IEvent {
    public object Value => value;
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ReceivedObjectEventArgs).Name;
}