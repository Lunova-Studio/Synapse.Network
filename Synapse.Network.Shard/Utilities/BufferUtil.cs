namespace Synapse.Network.Shared.Utilities;

public static class BufferUtil {
    public static bool CheckBufferArgs(byte[] buffer, int offset, int count) {
        return buffer.Length - offset >= count;
    }

    public static void ValidateBufferArguments(byte[] buffer, int offset, int count) {
        if (!CheckBufferArgs(buffer, offset, count))
            throw new ArgumentException();
    }
}
