namespace TphdCemuTrainer.Memory;

public static class BigEndianMemory
{
    public static bool TryReadUInt16(ProcessMemory memory, ulong address, out ushort value, out string error)
    {
        value = 0;

        if (!memory.TryReadBytes(address, 2, out var bytes, out var bytesRead) || bytesRead != 2)
        {
            error = $"Could not read 2 bytes at 0x{address:X}.";
            return false;
        }

        value = (ushort)((bytes[0] << 8) | bytes[1]);
        error = string.Empty;
        return true;
    }

    public static bool TryWriteUInt16(ProcessMemory memory, ulong address, ushort value, out string error)
    {
        var bytes = new[]
        {
            (byte)(value >> 8),
            (byte)(value & 0xFF)
        };

        return memory.TryWriteBytes(address, bytes, out error);
    }
}
