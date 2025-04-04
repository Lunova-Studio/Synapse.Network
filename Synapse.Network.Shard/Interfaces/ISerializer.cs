namespace Synapse.Network.Shard.Interfaces;

public interface ISerializer;

public interface ISerializer<T> : ISerializer {
    Span<byte> Serialize(T data);
    T Deserialize(Memory<byte> data);
}