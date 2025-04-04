using Synapse.Network.Shard.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace Synapse.Network.Shard.Events;

public sealed class NewUdpConnectionEventArgs : EventArgs, IEvent {
    public Socket Socket { get; set; }
    public IPEndPoint IPEndPoint { get; set; }

    public string EventName => typeof(NewUdpConnectionEventArgs).Name;

    public NewUdpConnectionEventArgs(IPEndPoint iPEndPoint, Socket socket) {
        IPEndPoint = iPEndPoint;
        Socket = socket;
    }
}