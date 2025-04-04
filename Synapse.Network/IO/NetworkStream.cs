using Synapse.Network.Shard.Utilities;
using System;
using System.Net.Sockets;

namespace Synapse.Network.IO;

public sealed class SafeNetworkStream : Stream {
    public NetworkStream NetworkStream { get; }
    public override bool CanSeek => NetworkStream.CanSeek;
    public override bool CanRead => NetworkStream.CanRead;
    public override bool CanWrite => NetworkStream.CanWrite;
    public override long Length => NetworkStream.Length;
    public override long Position {
        get => NetworkStream.Position;
        set => NetworkStream.Position = value;
    }

    public SafeNetworkStream(NetworkStream networkStream) {
        NetworkStream = networkStream ?? 
            throw new ArgumentNullException(nameof(networkStream));
    }

    public override void Flush() => NetworkStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) {
        BufferUtil.CheckBufferArgs(buffer, offset, count);

        int bytesRead = 0;
        while (bytesRead < count) {
            int read = NetworkStream.Read(buffer, offset + bytesRead, count - bytesRead);
            if (read == 0) {
                Task.Delay(1).Wait();
                continue;
            }

            bytesRead += read;
        }
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) {
        BufferUtil.CheckBufferArgs(buffer, offset, count);

        int bytesRead = 0;
        while (bytesRead < count) {
            int read = await NetworkStream.ReadAsync(buffer.AsMemory(offset + bytesRead, count - bytesRead), cancellationToken);
            if (read == 0) {
                await Task.Delay(1, cancellationToken);
                continue;
            }

            bytesRead += read;
        }

        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        int bytesRead = 0;
        while (bytesRead < buffer.Length) {
            int read = await NetworkStream.ReadAsync(buffer[bytesRead..].ToArray().AsMemory(0, buffer.Length - bytesRead), cancellationToken);
            if (read == 0) {
                await Task.Delay(1, cancellationToken);
                continue;
            }

            bytesRead += read;
        }

        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count) {
        NetworkStream.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) {
        await NetworkStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        await NetworkStream.WriteAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin) {
        return NetworkStream.Seek(offset, origin);
    }

    public override void SetLength(long value) {
        NetworkStream.SetLength(value);
    }

    protected override void Dispose(bool disposing) {
        if (disposing)
            NetworkStream.Dispose();

        base.Dispose(disposing);
    }
}