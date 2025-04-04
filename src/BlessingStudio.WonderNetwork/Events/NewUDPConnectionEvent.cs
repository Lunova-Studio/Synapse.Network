using System.Net;
using System.Net.Sockets;

namespace BlessingStudio.WonderNetwork.Events;

public sealed class NewUDPConnectionEvent : IEvent
{
    public IPEndPoint IPEndPoint { get; set; }
    public Socket Socket { get; set; }
    public NewUDPConnectionEvent(IPEndPoint iPEndPoint, Socket socket)
    {
        IPEndPoint = iPEndPoint;
        Socket = socket;
    }
}
