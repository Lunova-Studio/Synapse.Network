using Synapse.Network.Extensions;
using Synapse.Network.Shared.Enums;
using Synapse.Network.Shared.Events;
using Synapse.Network.Shared.Interfaces;
using Synapse.Network.Shared.Utilities;
using System.Collections;
using System.Collections.Concurrent;

namespace Synapse.Network;

public class Connection(Stream networkStream) : IConnection, IEnumerable<Channel> {
    private readonly Stream _networkStream = networkStream;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly ConcurrentDictionary<string, Channel> _channels = [];
    private readonly ConcurrentDictionary<string, INetworkHandler> _handlers = [];//此处使用 ConcurrentDictionary 而不采用 ConcurrentBag，理由是不支持移除指定元素。 
    private readonly Thread _pollingThread = new(new ParameterizedThreadStart(Polling!));

    public bool IsDisposed { get; private set; }
    public ConcurrentDictionary<Type, ISerializer> Serializer { get; private set; } = new();

    public event EventHandler<DisposedEventArgs>? Disposed;
    public event EventHandler<ReceivedBytesEventArgs>? ReceivedBytes;
    public event EventHandler<ReceivedObjectEventArgs>? ReceivedObject;
    public event EventHandler<ChannelCreatedEventArgs>? ChannelCreated;
    public event EventHandler<ChannelDeletedEventArgs>? ChannelDeleted;

    public void Run() {
        if (_pollingThread.ThreadState is
            ThreadState.Aborted or ThreadState.Unstarted or ThreadState.Stopped)
            _pollingThread.Start(this);
    }

    public async Task SendAsync(string name, Memory<byte> payload) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_channels.ContainsKey(name) && !payload.IsEmpty) {
            using MemoryStream memoryStream = new();
            memoryStream.WriteByte((byte)PacketType.SendChannelByteData);
            memoryStream.WriteString(name);
            memoryStream.WriteVarInt(payload.Length);
            await memoryStream.WriteAsync(payload);

            await _semaphoreSlim.WaitAsync();
            try {
                await _networkStream.WriteAsync(memoryStream.ToArray());
                await _networkStream.FlushAsync();
            } finally {
                _semaphoreSlim.Release();
            }
        }
    }

    public async Task SendAsync<T>(string name, T customPayload) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_channels.ContainsKey(name))
            throw new InvalidOperationException("Channel not found.");

        var type = typeof(T);
        var serializer = Serializer.FirstOrDefault(x =>
            type == x.Key || type.IsSubclassOf(x.Key) || type.GetInterfaces().Contains(x.Key)).Value
                ?? throw new ArgumentNullException("amns");

        //Experimental
        var buffer = (serializer as ISerializer<T>)!.Serialize(customPayload);
        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.SendChannelObjectData);
        memoryStream.WriteString(name);
        memoryStream.WriteString(type.FullName!);
        memoryStream.WriteVarInt(buffer!.Length);
        await memoryStream.WriteAsync(buffer.ToArray());

        await _semaphoreSlim.WaitAsync();
        try {
            await _networkStream.WriteAsync(memoryStream.ToArray());
            await _networkStream.FlushAsync();
        } finally {
            _semaphoreSlim.Release();
        }
    }

    public async Task<T?> WaitForAsync<T>(string name, TimeSpan timeout) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_channels.ContainsKey(name))
            throw new InvalidOperationException("Channel not found.");

        T? result = default;
        await TaskUtil.RunWithTimeout(async x => {
            result = await WaitForAsync<T>(name, x);
        }, async (x1) => await x1.CancelAsync(), timeout);

        return result;
    }

    public async Task<T?> WaitForAsync<T>(string name, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_channels.ContainsKey(name))
            throw new InvalidOperationException("Channel not found.");

        T? result = default;
        DefaultEventHandler handler = new();
        handler.ReceivedObject += (_, args) => {
            if (result is null && args.Channel.ChannelName == name && args.Value is T t)
                result = t;
        };

        AddHandler(handler, nameof(DefaultEventHandler));
        do {
            if (result is null)
                await Task.Delay(1, cancellationToken);
            else {
                RemoveHandler(nameof(DefaultEventHandler));
                return result;
            }
        } while (!cancellationToken.IsCancellationRequested);

        throw new OperationCanceledException(cancellationToken);
    }

    public async Task<IChannel> CreateChannelAsync(string name) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_channels.ContainsKey(name))
            return GetChannel(name);

        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.CreateChannel);
        memoryStream.WriteString(name);

        await _semaphoreSlim.WaitAsync();
        try {
            await _networkStream.WriteAsync(memoryStream.ToArray());
            await _networkStream.FlushAsync();
        } finally {
            _semaphoreSlim.Release();
        }

        if (_channels.TryAdd(name, new(this, name))) {
            ChannelCreated?.Invoke(this, new(GetChannel(name), this));
            return GetChannel(name);
        }

        throw new InvalidOperationException();
    }

    public async Task DeleteChannel(string name) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (!_channels.ContainsKey(name))
            throw new InvalidOperationException();

        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.DeleteChannel);
        memoryStream.WriteString(name);

        await _semaphoreSlim.WaitAsync();
        try {
            await _networkStream.WriteAsync(memoryStream.ToArray());
            await _networkStream.FlushAsync();
        } finally {
            _semaphoreSlim.Release();
        }

        if (_channels.TryRemove(name, out var channel)) {
            ChannelDeleted?.Invoke(this, new(channel, this));
        }
    }

    public async Task SendMeaninglessPacketAsync() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        await _semaphoreSlim.WaitAsync();
        try {
            _networkStream.WriteByte((byte)PacketType.Meaningless);
            await _networkStream.FlushAsync();
        } finally {
            _semaphoreSlim.Release();
        }
    }

    public IChannel GetChannel(string name) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_channels.TryGetValue(name, out var channel))
            return channel;

        throw new InvalidOperationException("Not found the channel.");
    }

    public void HandleEventHandler(IEvent eventType) {
        foreach (var handler in _handlers.Values) {
            switch (eventType) {
                case ReceivedBytesEventArgs bytesEvent:
                    handler.OnReceivedBytes(bytesEvent);
                    break;
                case ReceivedObjectEventArgs objectEvent:
                    handler.OnReceivedObject(objectEvent);
                    break;
                case DisposedEventArgs disposedEvent:
                    handler.OnDisposed(disposedEvent);
                    break;
                case ChannelCreatedEventArgs createdEvent:
                    handler.OnChannelCreated(createdEvent);
                    break;
                case ChannelDeletedEventArgs deletedEvent:
                    handler.OnChannelDeleted(deletedEvent);
                    break;
            }
        }
    }

    public void AddHandler(INetworkHandler handler, string key) {
        _handlers.TryAdd(key, handler);
    }

    public void RemoveHandler(string key) {
        _handlers.TryRemove(key, out var _);
    }

    public IEnumerator<Channel> GetEnumerator() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _channels.Values.GetEnumerator();
    }

    public void Dispose() {
        if (IsDisposed)
            return;

        IsDisposed = true;
        _networkStream.Dispose();
        Disposed?.Invoke(this, new(this));
        GC.SuppressFinalize(this);
    }

    public static void Polling(object arg) {
        var connection = (arg as Connection)!;
        var networkStream = connection._networkStream;

        try {
            while (true) {
                PacketType packetType = (PacketType)networkStream.ReadByte();
                ObjectDisposedException.ThrowIf(connection.IsDisposed, connection);

                switch (packetType) {
                    case PacketType.CreateChannel:
                        HandleCreateChannel(networkStream, connection);
                        break;
                    case PacketType.DeleteChannel:
                        HandleDeleteChannel(networkStream, connection);
                        break;
                    case PacketType.SendChannelByteData:
                        HandleSendByteChannel(networkStream, connection);
                        break;
                    case PacketType.SendChannelObjectData:
                        HandleSendCustomChannel(networkStream, connection);
                        break;
                    default:
                        break;
                }
            }
        } catch (Exception) {
            connection.Dispose();
        }
    }

    private static void HandleCreateChannel(Stream stream, Connection connection) {
        var name = stream.ReadString();

        if (connection._channels.ContainsKey(name))
            return;

        connection._channels.TryAdd(name, new(connection, name));
        var e = new ChannelCreatedEventArgs(connection.GetChannel(name), connection);

        connection.ChannelCreated?.Invoke(connection, e);
        connection.HandleEventHandler(e);
    }

    private static void HandleDeleteChannel(Stream stream, Connection connection) {
        var name = stream.ReadString();

        if (connection._channels.ContainsKey(name))
            return;

        connection._channels.TryRemove(name, out _);
        var e = new ChannelDeletedEventArgs(connection.GetChannel(name), connection);

        connection.ChannelDeleted?.Invoke(connection, e);
        connection.HandleEventHandler(e);
    }

    private static void HandleSendByteChannel(Stream stream, Connection connection) {
        var name = stream.ReadString();
        var length = stream.ReadVarInt();
        var data = new byte[length];
        stream.Read(data);
        if (connection._channels.ContainsKey(name)) {
            var channel = connection.GetChannel(name);
            ReceivedBytesEventArgs eventArgs = new(channel, connection, data);
            connection.ReceivedBytes?.Invoke(connection, eventArgs);
            connection.HandleEventHandler(eventArgs);
        }
    }

    private static void HandleSendCustomChannel(Stream stream, Connection connection) {
        var name = stream.ReadString();
        var typeName = stream.ReadString();
        var length = stream.ReadVarInt();
        var data = new byte[length];
        stream.Read(data);

        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var type = allAssemblies.Select(x => x.GetType(typeName))
            .FirstOrDefault(x => x is not null);

        var serializer = connection.Serializer.FirstOrDefault(x =>
            type == x.Key || type!.IsSubclassOf(x.Key) || type.GetInterfaces().Contains(x.Key)).Value
                ?? throw new ArgumentNullException(null, nameof(type));

        var obj = Deserilize(type!, serializer, data);
        if (connection._channels.ContainsKey(name)) {
            var channel = connection.GetChannel(name);
            ReceivedObjectEventArgs eventArgs = new(channel, connection, obj);
            connection.ReceivedObject?.Invoke(connection, eventArgs);
            connection.HandleEventHandler(eventArgs);
        }
    }

    private static object Deserilize(Type type, ISerializer serilizer, byte[] data) {
        var serilizerType = serilizer.GetType();
        var interfaceType = serilizerType.GetInterfaces()
            .FirstOrDefault(t =>
                t.GetGenericTypeDefinition() == typeof(ISerializer<>));

        if (interfaceType!.GenericTypeArguments.Length == 1) {
            var genericType = interfaceType.GenericTypeArguments[0];

            if (type == genericType || type.IsSubclassOf(genericType) || type.GetInterfaces().Contains(genericType)) {
                var methodInfo = interfaceType.GetMethod("Deserialize")!;
                return methodInfo.Invoke(serilizer, [data.AsMemory()])!;
            }

            throw new InvalidOperationException("Type is not the type or subclass in serilize");
        }

        throw new InvalidOperationException("Serilizer Error");
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}