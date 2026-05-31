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

    public static bool TryReadUInt32(ProcessMemory memory, ulong address, out uint value, out string error)
    {
        value = 0;

        if (!memory.TryReadBytes(address, 4, out var bytes, out var bytesRead) || bytesRead != 4)
        {
            error = $"Could not read 4 bytes at 0x{address:X}.";
            return false;
        }

        value =
            ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
        error = string.Empty;
        return true;
    }

    public static bool TryWriteUInt32(ProcessMemory memory, ulong address, uint value, out string error)
    {
        var bytes = new[]
        {
            (byte)(value >> 24),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF)
        };

        return memory.TryWriteBytes(address, bytes, out error);
    }
}
