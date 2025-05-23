﻿namespace Synapse.Network.Shared.Interfaces;

public interface IChannel {
    string ChannelName { get; }
    IConnection Connection { get; }

    Task SendAsync<T>(T obj);
    Task SendAsync(Memory<byte> data);
}