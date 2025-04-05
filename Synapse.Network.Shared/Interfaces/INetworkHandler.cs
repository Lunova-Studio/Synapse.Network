using Synapse.Network.Shared.Events;

namespace Synapse.Network.Shared.Interfaces;

public interface INetworkHandler {
    void OnDisposed(DisposedEventArgs @event);
    void OnReceivedBytes(ReceivedBytesEventArgs @event);
    void OnReceivedObject(ReceivedObjectEventArgs @event);
    void OnChannelCreated(ChannelCreatedEventArgs @event);
    void OnChannelDeleted(ChannelDeletedEventArgs @event);
}