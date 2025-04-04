using BlessingStudio.WonderNetwork;
using BlessingStudio.WonderNetwork.Threading;
using System.Net.Sockets;

namespace SendingTest
{
    public class UDPTests
    {
        public Connection ServerConnection;
        public Connection ClientConnection;
        UdpClient udpClient;
        UdpClient udpServer;
        UDPReceiver sreceiver;
        [SetUp]
        public void Setup()
        {
            int port = new Random().Next(10000, 50000);
            udpServer = new UdpClient(port);
            sreceiver = new(udpServer.Client);
            sreceiver.NewUDPConnection += (e) =>
            {
                ClientConnection = new(sreceiver.NetworkStreams[e.IPEndPoint]);
                ClientConnection.Start();
            };
            udpClient = new UdpClient();
            udpClient.Connect("localhost", port);
            ServerConnection = new(new UDPNetworkStream(udpClient.Client));
            ServerConnection.CreateChannel("_");
            ServerConnection.Start();
            Thread.Sleep(500);
        }

        [Test]
        public void BytesSendingTest()
        {
            bool pass = false;
            byte[] bytes = { 1, 2, 3, 4 };

            ServerConnection.ReceivedBytes += (e) =>
            {
                bool equal = bytes.Length == e.Data.Length;
                if (equal)
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (bytes[i] != e.Data[i])
                        {
                            equal = false;
                            break;
                        }
                    }
                }
                pass = equal;
            };
            Channel channel = ClientConnection.CreateChannel("master");
            channel.Send(bytes);
            Thread.Sleep(1000);
            if (pass)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }
        [Test]
        public void ObjectSendingTest()
        {
            bool pass = false;
            Class1 class1 = new Class1()
            {
                Property1 = "hello",
                Property2 = " ",
                field1 = "",
                field2 = ", world"
            };
            ServerConnection.ReceivedObject += (e) =>
            {
                pass = class1.Equals(e.Object);
            };
            ServerConnection.Serializers[typeof(Class1)] = new Class1Serilizer();
            ClientConnection.Serializers[typeof(Class1)] = new Class1Serilizer();
            Channel channel = ClientConnection.CreateChannel("master");
            channel.Send(class1);
            Thread.Sleep(100);
            if (pass)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}