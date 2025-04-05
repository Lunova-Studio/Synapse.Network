using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Shared.Events;

public sealed class ReceivedBytesEventArgs(IChannel channel, IConnection connection, byte[] data) : EventArgs, IEvent {
    public Span<byte> Data => data;
    public IChannel Channel => channel;
    public IConnection Connection => connection;
    public string EventName => typeof(ReceivedBytesEventArgs).Name;
}