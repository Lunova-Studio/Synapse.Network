using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Shared.Events;
public sealed class DisposedEventArgs(IConnection connection) : EventArgs, IEvent {
    public IConnection Connection => connection;
    public string EventName => typeof(DisposedEventArgs).Name;
}