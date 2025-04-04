using BlessingStudio.WonderNetwork.Extensions;
using BlessingStudio.WonderNetwork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendingTest
{
    public class Class1
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        public string field1;
        public string field2;
        public override bool Equals(object? obj)
        {
            if(obj is Class1)
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
}
