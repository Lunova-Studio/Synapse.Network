using BlessingStudio.WonderNetwork.Events;
using BlessingStudio.WonderNetwork.Interfaces;

namespace BlessingStudio.WonderNetwork;

public class SimpleHandler : IHandler
{
    public event Events.EventHandler<ReceivedBytesEvent>? ReceivedBytes;
    public event Events.EventHandler<ReceivedObjectEvent>? ReceivedObject;
    public event Events.EventHandler<ChannelCreatedEvent>? ChannelCreated;
    public event Events.EventHandler<ChannelDeletedEvent>? ChannelDeleted;
    public event Events.EventHandler<DisposedEvent>? Disposed;
    public void OnChannelCreated(ChannelCreatedEvent @event)
    {
        ChannelCreated?.Invoke(@event);
    }

    public void OnChannelDeleted(ChannelDeletedEvent @event)
    {
        ChannelDeleted?.Invoke(@event);
    }

    public void OnDisposed(DisposedEvent @event)
    {
        Disposed?.Invoke(@event);
    }

    public void OnReceivedBytes(ReceivedBytesEvent @event)
    {
        ReceivedBytes?.Invoke(@event);
    }

    public void OnReceivedObject(ReceivedObjectEvent @event)
    {
        ReceivedObject?.Invoke(@event);
    }
}
