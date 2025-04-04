using BlessingStudio.WonderNetwork.Enums;
using BlessingStudio.WonderNetwork.Events;
using BlessingStudio.WonderNetwork.Extensions;
using BlessingStudio.WonderNetwork.Interfaces;
using BlessingStudio.WonderNetwork.Utils;
using System.Collections;
using System.Reflection;

namespace BlessingStudio.WonderNetwork;

public class Connection : IConnection, IEnumerable<Channel>
{
    private readonly Stream networkStream;
    private readonly Thread ReceivingThread = new(new ParameterizedThreadStart(Listening));
    public object sendingLock = new();
    private readonly List<string> channels = new();
    public Dictionary<Type, ISerializer> Serializers { get; private set; } = new();
    public bool IsDisposed { get; private set; }
    public event Events.EventHandler<ReceivedBytesEvent>? ReceivedBytes;
    public event Events.EventHandler<ReceivedObjectEvent>? ReceivedObject;
    public event Events.EventHandler<ChannelCreatedEvent>? ChannelCreated;
    public event Events.EventHandler<ChannelDeletedEvent>? ChannelDeleted;
    public event Events.EventHandler<DisposedEvent>? Disposed;
    public List<IHandler> Handlers { get; } = new List<IHandler>();
    public Connection(Stream networkStream)
    {
        this.networkStream = networkStream;
    }
    public void Send(string channelName, byte[] data)
    {
        CheckDisposed();
        if (channels.Contains(channelName) && data.Length != 0)
        {
            using MemoryStream memoryStream = new();
            memoryStream.WriteByte((byte)PacketType.SendChannelByteData);
            memoryStream.WriteString(channelName);
            memoryStream.WriteVarInt(data.Length);
            memoryStream.Write(data);
            lock (sendingLock)
            {
                networkStream.Write(memoryStream.ToArray());
                networkStream.Flush();
            }
        }
    }
    public async Task SendAsync(string channelName, byte[] data)
    {
        await Task.Run(() => Send(channelName, data));
    }
    public void Send<T>(string channelName, T data)
    {
        CheckDisposed();
        if (!channels.Contains(channelName))
        {
            throw new InvalidOperationException();
        }
        Type type = typeof(T);
        ISerializer? serializer = default;
        if (Serializers.ContainsKey(type))
        {
            serializer = Serializers[type];
        }
        else
        {
            foreach (KeyValuePair<Type, ISerializer> keyValuePair in Serializers)
            {
                if (type.IsSubclassOf(keyValuePair.Key) || type.GetInterfaces().Contains(keyValuePair.Key))
                {
                    serializer = keyValuePair.Value;
                    break;
                }
            }
        }
        if (serializer == null)
        {
            throw new InvalidOperationException("Serilizer Not Found");
        }
        MethodInfo methodInfo = serializer.GetType().GetMethod("Serialize")!;
        byte[] buffer = (byte[])methodInfo.Invoke(serializer, new object[] { data! })!;
        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.SendChannelObjectData);
        memoryStream.WriteString(channelName);
        memoryStream.WriteString(type.FullName!);
        memoryStream.WriteVarInt(buffer.Length);
        memoryStream.Write(buffer);
        lock (sendingLock)
        {
            networkStream.Write(memoryStream.ToArray());
            networkStream.Flush();
        }
    }
    public T? WaitFor<T>(string channelName, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (!channels.Contains(channelName))
        {
            throw new InvalidOperationException();
        }
        T? result = default;
        SimpleHandler handler = new();
        handler.ReceivedObject += a;
        void a(ReceivedObjectEvent @event)
        {
            if (result == null && @event.Channel.ChannelName == channelName && @event.Object is T t)
            {
                result = t;
            }
        }
        AddHandler(handler);
        while (true)
        {
            if (result != null)
            {
                RemoveHandler(handler);
                return result!;
            }
            else
            {
                Thread.Sleep(1);
            }
            if (cancellationToken.IsCancellationRequested) return result;
        }
    }
    public T? WaitFor<T>(string channelName, TimeSpan timeout)
    {
        CheckDisposed();
        if (!channels.Contains(channelName))
        {
            throw new InvalidOperationException();
        }
        T? result = default;
        ThreadUtils.Run((c) =>
        {
            result = WaitFor<T>(channelName, c);
        }, timeout);
        return result;
    }
    public Channel CreateChannel(string name)
    {
        CheckDisposed();
        if (channels.Contains(name))
        {
            return GetChannel(name);
        }
        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.CreateChannel);
        memoryStream.WriteString(name);
        lock (sendingLock)
        {
            networkStream.Write(memoryStream.ToArray());
            networkStream.Flush();
        }
        channels.Add(name);
        ChannelCreated?.Invoke(new(GetChannel(name), this));
        return GetChannel(name);
    }

    public void DestroyChannel(string name)
    {
        CheckDisposed();
        if (!channels.Contains(name))
        {
            throw new InvalidOperationException();
        }
        using MemoryStream memoryStream = new();
        memoryStream.WriteByte((byte)PacketType.DestroyChannel);
        memoryStream.WriteString(name);
        lock (sendingLock)
        {
            networkStream.Write(memoryStream.ToArray());
            networkStream.Flush();
        }
        channels.Remove(name);
        ChannelDeleted?.Invoke(new(name, this));
    }

    public Channel GetChannel(string name)
    {
        CheckDisposed();
        if (channels.Contains(name))
        {
            return new(this, name);
        }
        throw new InvalidOperationException("Not Found");
    }

    public void SendMeaninglessPacket()
    {
        CheckDisposed();
        lock (sendingLock)
        {
            networkStream.WriteByte((byte)PacketType.Meaningless);
            networkStream.Flush();
        }
    }

    public IReadOnlyList<Channel> GetChannels()
    {
        CheckDisposed();
        List<Channel> channels = new();
        foreach (string channel in this.channels)
        {
            channels.Add(GetChannel(channel));
        }
        return channels;
    }
    public static void Listening(object? arg)
    {
        Connection connection = (Connection)arg!;
        Stream networkStream = connection.networkStream;
        try
        {
            while (true)
            {
                PacketType packetType = (PacketType)networkStream.ReadByte();
                connection.CheckDisposed();
                switch (packetType)
                {
                    case PacketType.CreateChannel:
                        {
                            string name = networkStream.ReadString();
                            if (!connection.channels.Contains(name))
                            {
                                connection.channels.Add(name);
                                ChannelCreatedEvent e = new(connection.GetChannel(name), connection);
                                connection.ChannelCreated?.Invoke(e);
                                connection.CallEventToHandlers(e);
                            }
                        }
                        break;
                    case PacketType.DestroyChannel:
                        {
                            string name = networkStream.ReadString();
                            if (connection.channels.Contains(name))
                            {
                                connection.channels.Remove(name);
                                ChannelDeletedEvent e = new(name, connection);
                                connection.ChannelDeleted?.Invoke(e);
                                connection.CallEventToHandlers(e);
                            }
                        }
                        break;
                    case PacketType.SendChannelByteData:
                        {
                            string name = networkStream.ReadString();
                            int length = networkStream.ReadVarInt();
                            byte[] data = new byte[length];
                            networkStream.Read(data);
                            if (connection.channels.Contains(name))
                            {
                                Channel channel = connection.GetChannel(name);
                                ReceivedBytesEvent @event = new(channel, connection, data);
                                connection.ReceivedBytes?.Invoke(@event);
                                connection.CallEventToHandlers(@event);
                            }
                        }
                        break;
                    case PacketType.SendChannelObjectData:
                        {
                            string name = networkStream.ReadString();
                            string typeName = networkStream.ReadString();
                            int length = networkStream.ReadVarInt();
                            byte[] data = new byte[length];
                            networkStream.Read(data);
                            Type? type = ReflectionUtils.GetType(typeName);
                            if (type == null)
                            {
                                break;
                            }
                            ISerializer? serilizer = default;
                            if (connection.Serializers.ContainsKey(type))
                            {
                                serilizer = connection.Serializers[type];
                            }
                            else
                            {
                                foreach (KeyValuePair<Type, ISerializer> keyValuePair in connection.Serializers)
                                {
                                    if (type.IsSubclassOf(keyValuePair.Key) || type.GetInterfaces().Contains(keyValuePair.Key))
                                    {
                                        serilizer = keyValuePair.Value;
                                        break;
                                    }
                                }
                            }
                            if (serilizer == null)
                            {
                                break;
                            }
                            object @object = ReflectionUtils.Deserilize(type, serilizer, data);
                            if (connection.channels.Contains(name))
                            {
                                Channel channel = connection.GetChannel(name);
                                ReceivedObjectEvent e = new(channel, connection, @object);
                                connection.ReceivedObject?.Invoke(e);
                                connection.CallEventToHandlers(e);
                            }
                        }
                        break;
                    default: break;
                }
            }
        }
        catch
        {
            connection.Dispose();
        }
    }
    public void Start()
    {
        if (ReceivingThread.ThreadState == ThreadState.Unstarted ||
            ReceivingThread.ThreadState == ThreadState.Stopped ||
            ReceivingThread.ThreadState == ThreadState.Aborted
            )
            ReceivingThread.Start(this);
    }
    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            networkStream.Dispose();
            Disposed?.Invoke(new(this));
            GC.SuppressFinalize(this);
        }
    }
    private void CheckDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    public IEnumerator<Channel> GetEnumerator()
    {
        return GetChannels().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetChannels().GetEnumerator();
    }

    public void AddHandler(Events.EventHandler<ReceivedBytesEvent> handler)
    {
        ReceivedBytes += handler;
    }

    public void AddHandler(Events.EventHandler<ReceivedObjectEvent> handler)
    {
        ReceivedObject += handler;
    }

    public void AddHandler(Events.EventHandler<DisposedEvent> handler)
    {
        Disposed += handler;
    }

    public void AddHandler(Events.EventHandler<ChannelCreatedEvent> handler)
    {
        ChannelCreated += handler;
    }

    public void AddHandler(Events.EventHandler<ChannelDeletedEvent> handler)
    {
        ChannelDeleted += handler;
    }

    public void AddHandler(IHandler handler)
    {
        lock (Handlers)
            Handlers.Add(handler);
    }

    public void RemoveHandler(IHandler handler)
    {
        lock (Handlers)
            Handlers.Remove(handler);
    }

    private void CallEventToHandlers(IEvent @event)
    {
        lock (Handlers)
            foreach (IHandler handler in Handlers)
        {
            if (@event is ReceivedBytesEvent)
            {
                handler.OnReceivedBytes((ReceivedBytesEvent)@event);
            }
            if (@event is ReceivedObjectEvent)
            {
                handler.OnReceivedObject((ReceivedObjectEvent)@event);
            }
            if (@event is DisposedEvent)
            {
                handler.OnDisposed((DisposedEvent)@event);
            }
            if (@event is ChannelCreatedEvent)
            {
                handler.OnChannelCreated((ChannelCreatedEvent)@event);
            }
            if (@event is ChannelDeletedEvent)
            {
                handler.OnChannelDeleted((ChannelDeletedEvent)@event);
            }
        }
    }
}
