using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.Memory;

public static class EquipmentMemoryService
{
    public static bool TryReadEquipped(
        ProcessMemory memory,
        ulong playerBaseAddress,
        EquipmentSlotDefinition slot,
        out byte value,
        out string error)
    {
        return TryReadByte(memory, playerBaseAddress, slot.Offset, slot.Name, out value, out error);
    }

    public static bool TryWriteEquipped(
        ProcessMemory memory,
        ulong playerBaseAddress,
        EquipmentSlotDefinition slot,
        byte value,
        out string error)
    {
        return TryWriteByte(memory, playerBaseAddress, slot.Offset, value, slot.Name, out error);
    }

    public static bool TryReadFlag(
        ProcessMemory memory,
        ulong playerBaseAddress,
        EquipmentFlagDefinition flag,
        out bool isOwned,
        out byte backingValue,
        out string error)
    {
        isOwned = false;

        if (!TryReadByte(memory, playerBaseAddress, flag.Offset, flag.Name, out backingValue, out error))
        {
            return false;
        }

        isOwned = (backingValue & flag.Mask) != 0;
        return true;
    }

    public static bool TryWriteFlag(
        ProcessMemory memory,
        ulong playerBaseAddress,
        EquipmentFlagDefinition flag,
        bool isOwned,
        out byte oldBackingValue,
        out byte writtenBackingValue,
        out string error)
    {
        oldBackingValue = 0;
        writtenBackingValue = 0;

        if (!TryReadByte(memory, playerBaseAddress, flag.Offset, flag.Name, out oldBackingValue, out error))
        {
            return false;
        }

        writtenBackingValue = isOwned
            ? (byte)(oldBackingValue | flag.Mask)
            : (byte)(oldBackingValue & ~flag.Mask);

        return TryWriteByte(memory, playerBaseAddress, flag.Offset, writtenBackingValue, flag.Name, out error);
    }

    public static bool TryReadByte(
        ProcessMemory memory,
        ulong playerBaseAddress,
        uint offset,
        string label,
        out byte value,
        out string error)
    {
        value = 0;
        error = string.Empty;

        var address = playerBaseAddress + offset;
        if (!memory.TryReadBytes(address, 1, out var bytes, out var bytesRead) || bytesRead != 1)
        {
            error = $"Could not read {label} at 0x{address:X}.";
            return false;
        }

        value = bytes[0];
        return true;
    }

    private static bool TryWriteByte(
        ProcessMemory memory,
        ulong playerBaseAddress,
        uint offset,
        byte value,
        string label,
        out string error)
    {
        var address = playerBaseAddress + offset;
        if (!memory.TryWriteBytes(address, [value], out error))
        {
            error = $"Could not write {label} at 0x{address:X}: {error}";
            return false;
        }

        return true;
    }
}
