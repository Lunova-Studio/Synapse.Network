namespace Synapse.Network.Shard.Enums;

public enum PacketType {
    CreateChannel,
    DeleteChannel,
    SendChannelByteData,
    SendChannelObjectData,
    Meaningless
}