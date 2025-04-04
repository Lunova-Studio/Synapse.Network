using System.Net.Sockets;

namespace Synapse.Network.Test.Threading;

public sealed class WaitTests {
    private Connection _server = null!;
    private Connection _client = null!;

    [SetUp]
    public async Task Setup() {
        int port = new Random().Next(10000, 50000);
        var tcpListener = TcpListener.Create(port);
        tcpListener.Start();

        await Task.Run(() => {
            TcpClient tcpClient = new();
            tcpClient.Connect("localhost", port);
            _server = new(tcpClient.GetStream());
            _server.Run();
        });

        var tcpClient = tcpListener.AcceptTcpClient();
        _client = new(tcpClient.GetStream());
        _client.Run();
    }

    [Test]
    public async Task WaitTest() {
        bool pass = false;
        CustomPayload payload = new() {
            Property1 = "123",
            Property2 = "456",
            field1 = "123",
            field2 = "456",
        };

        _server.Serializer[typeof(CustomPayload)] = new CustomPayloadSerilizer();
        _client.Serializer[typeof(CustomPayload)] = new CustomPayloadSerilizer();

        var channel = await _client.CreateChannelAsync("main");
        await Task.Run(async () => {
            var obj = await _client.WaitForAsync<CustomPayload>("main");
            if (obj!.Equals(payload)) {
                pass = true;
            }
        });

        await Task.Delay(TimeSpan.FromSeconds(2));
        await (await _server.CreateChannelAsync("main")).SendAsync(payload);
        await Task.Delay(TimeSpan.FromSeconds(3));

        if (pass)
            Assert.Pass();
        else
            Assert.Fail();
    }
}
