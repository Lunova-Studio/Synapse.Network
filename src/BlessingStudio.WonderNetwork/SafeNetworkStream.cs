using BlessingStudio.WonderNetwork.Utils;
using System.Net.Sockets;

namespace BlessingStudio.WonderNetwork;

public class SafeNetworkStream : Stream
{
    public NetworkStream NetworkStream { get; private set; }

    public override bool CanRead => NetworkStream.CanRead;

    public override bool CanSeek => NetworkStream.CanSeek;

    public override bool CanWrite => NetworkStream.CanWrite;

    public override long Length => NetworkStream.Length;

    public override long Position { get => NetworkStream.Position; set => NetworkStream.Position = value; }

    public SafeNetworkStream(NetworkStream networkStream)
    {
        NetworkStream = networkStream;
    }

    public override void Flush()
    {
        NetworkStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        BufferUtils.CheckBufferArgs(buffer, offset, count);
        using MemoryStream stream = new MemoryStream();
        byte[] bytes = new byte[count];
        int rest = count;
        while (rest != 0)
        {
            int c = NetworkStream.Read(bytes, 0, rest);
            if (c == 0)
            {
                Thread.Sleep(1);
                continue;
            }
            rest -= c;
            stream.Write(bytes, 0, c);
        }
        Array.Copy(stream.ToArray(), buffer, count);
        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return NetworkStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        NetworkStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        NetworkStream.Write(buffer, offset, count);
    }

    public override void Close()
    {
        NetworkStream.Dispose();
    }
}
