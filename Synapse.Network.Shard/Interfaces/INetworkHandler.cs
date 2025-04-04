using Synapse.Network.Shard.Events;

namespace Synapse.Network.Shard.Interfaces;

public interface INetworkHandler {
    void OnDisposed(DisposedEventArgs @event);
    void OnReceivedBytes(ReceivedBytesEventArgs @event);
    void OnReceivedObject(ReceivedObjectEventArgs @event);
    void OnChannelCreated(ChannelCreatedEventArgs @event);
    void OnChannelDeleted(ChannelDeletedEventArgs @event);
}