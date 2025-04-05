
namespace Synapse.Network.Shared.Interfaces;

//public interface IConnection : IDisposable {
//    Channel GetChannel(string name);
//    Channel CreateChannel(string name);

//    void DestroyChannel(string name);
//    void Send<T>(string channelName, T obj);
//    void Send(string channelName, ReadOnlySpan<byte> data);

//    Task SendAsync(string channelName, byte[] data);

//    void AddHandler(INetworkHandler handler);
//    void RemoveHandler(INetworkHandler handler);

//    Task<T?> WaitForAsync<T>(string channelName, TimeSpan timeout);
//    Task<T?> WaitForAsync<T>(string channelName, CancellationToken cancellationToken = default);

//    IReadOnlyCollection<Channel> GetChannels();
//}

public interface IConnection : IDisposable {
    IChannel GetChannel(string name);
    Task<IChannel> CreateChannelAsync(string name);

    Task SendAsync(string name, Memory<byte> payload);
    Task SendAsync<T>(string name, T customPayload);

    Task<T?> WaitForAsync<T>(string name, TimeSpan timeout);
    Task<T?> WaitForAsync<T>(string name, CancellationToken cancellationToken = default);
}