using Synapse.Network.Shared.Utilities;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Synapse.Network.IO;

public sealed class UdpNetworkStream : Stream, IDisposable {
    public IPEndPoint IPEndPoint { get; set; }
    public bool ConnectionToServer { get; set; } = false;
    public int BufferSize { get; set; } = 4 * 1024;

    private bool _isDisposed = false;
    private readonly Socket _socket;
    private readonly MemoryStream _sendBuffer = new();
    private readonly ConcurrentQueue<byte> _receiveBuffer = new();

    public UdpNetworkStream(Socket socket) : this(socket, default!) {
        ConnectionToServer = true;
    }

    public UdpNetworkStream(Socket socket, IPEndPoint endPoint) {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        IPEndPoint = endPoint;

        if (socket.ProtocolType != ProtocolType.Udp)
            throw new ArgumentException("Socket must use Udp protocol.");
    }

    public override bool CanSeek => false;
    public override bool CanRead => _socket.Connected;
    public override bool CanWrite => _socket.Connected;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _sendBuffer.Position = 0;
        byte[] buffer = _sendBuffer.ToArray();
        _sendBuffer.SetLength(0);

        if (ConnectionToServer)
            _socket.Send(buffer, SocketFlags.None);
        else if (IPEndPoint != null)
            _socket.SendTo(buffer, SocketFlags.None, IPEndPoint);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) {
        //if (_isDisposed) throw new ObjectDisposedException(nameof(UdpClient));
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ValidateBufferArguments(buffer, offset, count);

        while (_receiveBuffer.Count < count) {
            byte[] tempBuffer = new byte[BufferSize];
            int receivedCount = await Task.Run(() => _socket.Receive(tempBuffer));

            for (int i = 0; i < receivedCount; i++)
                _receiveBuffer.Enqueue(tempBuffer[i]);
        }

        for (int i = 0; i < count; i++) {
            if (!_receiveBuffer.TryDequeue(out byte b))
                break;

            buffer[offset + i] = b;
        }

        return count;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        int totalBytesRead = 0;
        while (buffer.Length > totalBytesRead) {
            if (_receiveBuffer.IsEmpty) {
                byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try {
                    int bytesReceived = await _socket.ReceiveAsync(tempBuffer.AsMemory(0, BufferSize),
                        SocketFlags.None, cancellationToken);

                    if (bytesReceived == 0)
                        return totalBytesRead;

                    for (int i = 0; i < bytesReceived; i++)
                        _receiveBuffer.Enqueue(tempBuffer[i]);
                } finally {
                    ArrayPool<byte>.Shared.Return(tempBuffer);
                }
            }

            while (!_receiveBuffer.IsEmpty && buffer.Length > totalBytesRead) {
                if (!_receiveBuffer.TryDequeue(out byte b))
                    break;

                buffer.Span[totalBytesRead++] = b;
            }
        }

        return totalBytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        lock (_sendBuffer) {
            _sendBuffer.Write(buffer, offset, count);
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        lock (_sendBuffer) {
            _sendBuffer.Write(buffer);
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        await Task.Run(() => {
            lock (_sendBuffer) {
                _sendBuffer.Write(buffer, offset, count);
            }
        }, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        return new ValueTask(Task.Run(() => {
            lock (_sendBuffer) {
                _sendBuffer.Write(buffer.Span);
            }
        }, cancellationToken));
    }

    public override long Seek(long offset, SeekOrigin origin) {
        throw new NotSupportedException();
    }

    public override void SetLength(long value) {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) {
        ValidateBufferArguments(buffer, offset, count);

        if (ConnectionToServer) {
            while (this._receiveBuffer.Count < count) {
                try {
                    byte[] bytes = new byte[BufferSize];
                    int c = _socket.Receive(bytes);
                    using MemoryStream memoryStream = new(bytes);
                    byte[] bytes1 = new byte[c];
                    memoryStream.Read(bytes1);
                    OnReceive(bytes1);
                } catch {
                    Thread.Sleep(1);
                }
            }
            for (int i = 0; i < count; i++) {
                this._receiveBuffer.TryDequeue(out buffer[i + offset]);
            }
            return count;
        }
        while (true) {
            if (this._receiveBuffer.Count >= count) {
                for (int i = 0; i < count; i++) {
                    this._receiveBuffer.TryDequeue(out buffer[i + offset]);
                }
                return count;
            }
            Thread.Sleep(2);
        }
    }

    protected override void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) {
                _sendBuffer.Dispose();
                _socket.Dispose();
            }

            _isDisposed = true;
            base.Dispose(disposing);
        }
    }

    public void OnReceive(byte[] data) {
        foreach (byte b in data)
            _receiveBuffer.Enqueue(b);
    }

    private new static void ValidateBufferArguments(byte[] buffer, int offset, int count) {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer arguments are out of range.");
    }
}