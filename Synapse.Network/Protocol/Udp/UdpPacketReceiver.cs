using Synapse.Network.IO;
using Synapse.Network.Shard.Events;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Synapse.Network.Protocol.Udp;

public class UdpPacketReceiver : IDisposable {
    private readonly List<Task> _tasks = [];
    private readonly object _threadLock = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public int ThreadCount => _tasks.Count;
    public int Buffersize { get; set; } = 4 * 1024;
    public Socket Socket { get; private set; }
    public ConcurrentDictionary<IPEndPoint, UdpNetworkStream> NetworkStreams { get; private set; } = new();

    public event EventHandler<NewUdpConnectionEventArgs>? NewUdpConnectionEstablished;

    public static int DefaultThreadCount { get; } = 4;

    public UdpPacketReceiver(Socket socket) {
        Socket = socket;
        Socket.Blocking = false;

        for (int i = 0; i < DefaultThreadCount; i++)
            AddReceivingTask();
    }

    public void SetThreadCount(int count) {
        if (count <= 0) 
            throw new ArgumentException(null, nameof(count));

        if (count < ThreadCount) {
            _cancellationTokenSource.Cancel();
            _tasks.RemoveRange(ThreadCount - count, count);
        } else if (count > ThreadCount)
            for (int i = ThreadCount; i < count; i++)
                AddReceivingTask();
    }

    public void Dispose() {
        _cancellationTokenSource.Cancel();
        foreach (var task in _tasks)
            task.Dispose();

        Socket.Close();
        GC.SuppressFinalize(this);
    }

    private void AddReceivingTask() {
        _tasks.Add(Task.Factory.StartNew(() =>
            ReceivingTaskAsync(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning));
    }

    private async Task ReceivingTaskAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] bytes = new byte[Buffersize];
            int count;

            try {
                if (Socket.Poll(10000, SelectMode.SelectRead))
                    count = Socket.ReceiveFrom(bytes, ref endPoint);
                else {
                    await Task.Delay(10, cancellationToken);
                    continue;
                }
            } catch (Exception) {
                await Task.Delay(10, cancellationToken);
                continue;
            }

            lock (_threadLock) {
                IPEndPoint iPEndPoint = (IPEndPoint)endPoint;
                byte[] buffer = new byte[count];
                Array.Copy(bytes, buffer, count);

                if (NetworkStreams.TryGetValue(iPEndPoint, out UdpNetworkStream? value)) {
                    value.OnReceive(buffer);
                } else {
                    NetworkStreams[iPEndPoint] = new UdpNetworkStream(Socket, iPEndPoint);
                    NewUdpConnectionEstablished?.Invoke(this, 
                        new NewUdpConnectionEventArgs(iPEndPoint, Socket));

                    NetworkStreams[iPEndPoint].OnReceive(buffer);
                }
            }
        }
    }
}