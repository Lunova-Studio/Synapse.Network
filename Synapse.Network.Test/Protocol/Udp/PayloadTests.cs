using Synapse.Network.IO;
using Synapse.Network.Protocol.Udp;
using System.Net.Sockets;

namespace Synapse.Network.Test.Protocol.Udp;

public sealed class PayloadTests {
    private Connection _server = null!;
    private Connection _client = null!;
    private UdpPacketReceiver _udppReceiver = null!;

    [SetUp]
    public async Task Setup() {
        var port = new Random().Next(14514);
        var udpServer = new UdpClient(port);

        _udppReceiver = new(udpServer.Client);
        _udppReceiver.NewUdpConnectionEstablished += (x, e) => {
            _client = new(_udppReceiver.NetworkStreams[e.IPEndPoint]);
            _client.Run();
        };

        var udpClient = new UdpClient();
        udpClient.Connect("localhost", port);

        _server = new(new UdpNetworkStream(udpClient.Client));
        await _server.CreateChannelAsync("_");
        _server.Run();

        await Task.Delay(TimeSpan.FromSeconds(0.5));
    }

    [Test]
    public async Task BytesSendingTest() {
        bool pass = false;
        var payload = new byte[] { 1, 2, 3, 4 }
            .AsMemory();

        _server.ReceivedBytes += (_, e) => {
            bool equal = payload.Length == e.Data.Length;
            if (equal)
                equal = payload.Span.SequenceEqual(e.Data);

            pass = equal;
        };

        var channel = await _client.CreateChannelAsync("master");
        await channel.SendAsync(payload);
        await Task.Delay(TimeSpan.FromSeconds(1));

        if (pass)
            Assert.Pass();
        else
            Assert.Fail();
    }

    [Test]
    public async Task CustomDataSendingTest() {
        bool pass = false;
        string passMessage = string.Empty;

        CustomPayload payload = new() {
            Property1 = "Fuck ",
            Property2 = "you",
            field1 = "",
            field2 = ", world"
        };

        _server.ReceivedObject += (_, e) => {
            pass = payload.Equals(e.Value);
            if (e.Value is CustomPayload cp) {
                passMessage = $"{cp.Property1}{cp.Property2}{cp.field1}{cp.field2}";
            }
        };

        _server.Serializer[typeof(CustomPayload)] = new CustomPayloadSerilizer();
        _client.Serializer[typeof(CustomPayload)] = new CustomPayloadSerilizer();

        var channel = await _client.CreateChannelAsync("master");
        await channel.SendAsync(payload);
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        if (pass)
            Assert.Pass(passMessage);
        else
            Assert.Fail();
    }
}