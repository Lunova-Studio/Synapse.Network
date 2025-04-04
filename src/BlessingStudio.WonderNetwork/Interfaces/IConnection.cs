using BlessingStudio.WonderNetwork.Events;

namespace BlessingStudio.WonderNetwork.Interfaces;

public interface IConnection : IDisposable
{
    public Channel CreateChannel(string name);
    public Channel GetChannel(string name);
    public IReadOnlyList<Channel> GetChannels();
    public void Send(string channelName, byte[] data);
    public void Send<T>(string channelName, T @object);
    public void DestroyChannel(string name);
    public void DestroyChannel(Channel channel)
    {
        DestroyChannel(channel.ChannelName);
    }
    public void AddHandler(Events.EventHandler<ReceivedBytesEvent> handler);
    public void AddHandler(Events.EventHandler<ReceivedObjectEvent> handler);
    public void AddHandler(Events.EventHandler<DisposedEvent> handler);
    public void AddHandler(Events.EventHandler<ChannelCreatedEvent> handler);
    public void AddHandler(Events.EventHandler<ChannelDeletedEvent> handler);
    public void AddHandler(IHandler handler);
    public void RemoveHandler(IHandler handler);
    public T? WaitFor<T>(string channelName, TimeSpan timeout);
    public T? WaitFor<T>(string channelName, CancellationToken cancellationToken = default);
}
