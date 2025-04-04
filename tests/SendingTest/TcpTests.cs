using BlessingStudio.WonderNetwork;
using System.Net.Sockets;

namespace SendingTest
{
    public class TcpTests
    {
        public Connection ServerConnection;
        public Connection ClientConnection;
        [SetUp]
        public void Setup()
        {
            int port = new Random().Next(10000, 50000);
            TcpListener tcpListener = new(port);
            tcpListener.Start();
            Task.Run(() =>
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect("localhost", port);
                ServerConnection = new(tcpClient.GetStream());
                ServerConnection.Start();
            });
            TcpClient tcpClient = tcpListener.AcceptTcpClient();
            ClientConnection = new(tcpClient.GetStream());
            ClientConnection.Start();
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