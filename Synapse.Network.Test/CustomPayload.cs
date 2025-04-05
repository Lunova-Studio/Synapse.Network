using Synapse.Network.Extensions;
using Synapse.Network.Shared.Interfaces;

namespace Synapse.Network.Test;

public sealed class CustomPayload {
    public string? Property1 { get; set; }
    public string? Property2 { get; set; }
    public string? field1;
    public string? field2;

    public override bool Equals(object? obj) {
        if (obj is CustomPayload payload)
            return Property1 == payload.Property1 && Property2 == payload.Property2 && field1 == payload.field1 && field2 == payload.field2;

        return false;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}

public class CustomPayloadSerilizer : ISerializer<CustomPayload> {
    public CustomPayload Deserialize(Memory<byte> data) {
        MemoryStream stream = new(data.ToArray());

        return new() {
            Property1 = stream.ReadString(),
            Property2 = stream.ReadString(),
            field1 = stream.ReadString(),
            field2 = stream.ReadString()
        };
    }

    public Span<byte> Serialize(CustomPayload payload) {
        MemoryStream stream = new();

        stream.WriteString(payload.Property1!);
        stream.WriteString(payload.Property2!);
        stream.WriteString(payload.field1!);
        stream.WriteString(payload.field2!);

        return stream.ToArray();
    }
}