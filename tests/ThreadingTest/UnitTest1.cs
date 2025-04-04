using BlessingStudio.WonderNetwork;
using BlessingStudio.WonderNetwork.Extensions;
using BlessingStudio.WonderNetwork.Interfaces;
using System.Net.Sockets;

namespace ThreadingTest
{
    public class Class1
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public string field1;
        public string field2;
        public override bool Equals(object? obj)
        {
            if (obj is Class1)
            {
                return Property1 == ((Class1)obj).Property1 && Property2 == ((Class1)obj).Property2 && field1 == ((Class1)obj).field1 && field2 == ((Class1)obj).field2;
            }
            return false;
        }
    }
    public class Class1Serilizer : ISerializer<Class1>
    {
        public Class1 Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            return new()
            {
                Property1 = stream.ReadString(),
                Property2 = stream.ReadString(),
                field1 = stream.ReadString(),
                field2 = stream.ReadString()
            };
        }

        public byte[] Serialize(Class1 @object)
        {
            MemoryStream stream = new MemoryStream();
            stream.WriteString(@object.Property1);
            stream.WriteString(@object.Property2);
            stream.WriteString(@object.field1);
            stream.WriteString(@object.field2);
            return stream.ToArray();
        }
    }
    public class Tests
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
        public void WaitingTest()
        {
            bool pass = false;
            Class1 class1 = new Class1()
            {
                Property1 = "123",
                Property2 = "456",
                field1 = "123",
                field2 = "456",
            };
            ServerConnection.Serializers[typeof(Class1)] = new Class1Serilizer();
            ClientConnection.Serializers[typeof(Class1)] = new Class1Serilizer();
            ClientConnection.CreateChannel("main");
            Task.Run(() =>
            {
                Class1 obj = ClientConnection.WaitFor<Class1>("main");
                if (obj.Equals(class1))
                {
                    pass = true;
                }
            });
            Thread.Sleep(2000);
            ServerConnection.CreateChannel("main").Send(class1);
            Thread.Sleep(3000);
            if (!pass)
            {
                Assert.Fail();
            }
        }
    }
}