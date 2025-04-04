using BlessingStudio.WonderNetwork.Utils;
using System.Net;
using System.Net.Sockets;

namespace BlessingStudio.WonderNetwork;

public partial class UDPNetworkStream : Stream, IDisposable
{
    public bool IsConnected
    {
        get
        {
            return Socket.Connected;
        }
    }
    public override bool CanRead => Socket.Connected;

    public override bool CanSeek => false;

    public override bool CanWrite => Socket.Connected;

    public override long Length => throw new NotSupportedException();

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush()
    {
        s_buffer.Position = 0;
        byte[] buffer = s_buffer.ToArray(); ;
        s_buffer.Flush();
        if (ConnectionToServer)
        {
            Socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            return;
        }
        Socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, IPEndPoint);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        BufferUtils.ValidateBufferArguments(buffer, offset, count);
        if (ConnectionToServer)
        {
            while (this.r_buffer.Count < count)
            {
                try
                {
                    byte[] bytes = new byte[Buffersize];
                    int c = Socket.Receive(bytes);
                    using MemoryStream memoryStream = new(bytes);
                    byte[] bytes1 = new byte[c];
                    memoryStream.Read(bytes1);
                    OnReceive(bytes1);
                }
                catch
                {
                    Thread.Sleep(1);
                }
            }
            for (int i = 0; i < count; i++)
            {
                buffer[i + offset] = this.r_buffer.Dequeue();
            }
            return count;
        }
        while (true)
        {
            if (this.r_buffer.Count >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[i + offset] = this.r_buffer.Dequeue();
                }
                return count;
            }
            Thread.Sleep(2);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (s_buffer)
        {
            s_buffer.Write(buffer, offset, count);
        }
    }
}
public partial class UDPNetworkStream : Stream, IDisposable
{
    public IPEndPoint IPEndPoint { get; set; }
    public bool ConnectionToServer { get; set; } = false;
    public int Buffersize { get; set; } = 4 * 1024;
    public bool IsDisposed { get; private set; } = false;
    public Socket Socket { get; private set; }
    private Queue<byte> r_buffer = new();
    private MemoryStream s_buffer = new();
    public UDPNetworkStream(Socket socket, IPEndPoint iPEndPoint)
    {
        this.Socket = socket;
        this.IPEndPoint = iPEndPoint;
        if (socket.ProtocolType != ProtocolType.Udp)
        {
            throw new ArgumentException();
        }
    }
    public UDPNetworkStream(Socket socket)
    {
        this.Socket = socket;
        ConnectionToServer = true;
        if (socket.ProtocolType != ProtocolType.Udp)
        {
            throw new ArgumentException();
        }
    }
    public void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
    public void OnReceive(byte[] data)
    {
        foreach (byte b in data)
        {
            r_buffer.Enqueue(b);
        }
    }
}
