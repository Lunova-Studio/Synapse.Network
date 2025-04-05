using Synapse.Network.Shared.Events;
using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network;

public class DefaultEventHandler : INetworkHandler {
    public event EventHandler<DisposedEventArgs>? Disposed;
    public event EventHandler<ReceivedBytesEventArgs>? ReceivedBytes;
    public event EventHandler<ReceivedObjectEventArgs>? ReceivedObject;
    public event EventHandler<ChannelCreatedEventArgs>? ChannelCreated;
    public event EventHandler<ChannelDeletedEventArgs>? ChannelDeleted;

    public void OnDisposed(DisposedEventArgs args) {
        Disposed?.Invoke(this, args);
    }

    public void OnReceivedBytes(ReceivedBytesEventArgs args) {
        ReceivedBytes?.Invoke(this, args);
    }

    public void OnReceivedObject(ReceivedObjectEventArgs args) {
        ReceivedObject?.Invoke(this, args);
    }

    public void OnChannelCreated(ChannelCreatedEventArgs args) {
        ChannelCreated?.Invoke(this, args);
    }

    public void OnChannelDeleted(ChannelDeletedEventArgs args) {
        ChannelDeleted?.Invoke(this, args);
    }
}
