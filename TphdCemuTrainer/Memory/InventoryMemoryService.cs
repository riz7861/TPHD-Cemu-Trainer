using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.Memory;

public static class InventoryMemoryService
{
    public static bool TryReadSlot(
        ProcessMemory memory,
        ulong playerBaseAddress,
        int slotIndex,
        out byte itemId,
        out string error)
    {
        itemId = InventoryDefinitions.EmptyItemId;
        error = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            error = $"Inventory slot {slotIndex + 1} is outside the CT-backed slot range.";
            return false;
        }

        var address = GetSlotAddress(playerBaseAddress, slotIndex);
        if (!memory.TryReadBytes(address, 1, out var bytes, out var bytesRead) || bytesRead != 1)
        {
            error = $"Could not read inventory slot {slotIndex + 1} at 0x{address:X}.";
            return false;
        }

        itemId = bytes[0];
        return true;
    }

    public static bool TryWriteSlot(
        ProcessMemory memory,
        ulong playerBaseAddress,
        int slotIndex,
        byte itemId,
        out string error)
    {
        error = string.Empty;

        if (!IsValidSlot(slotIndex))
        {
            error = $"Inventory slot {slotIndex + 1} is outside the CT-backed slot range.";
            return false;
        }

        var address = GetSlotAddress(playerBaseAddress, slotIndex);
        return memory.TryWriteBytes(address, [itemId], out error);
    }

    public static bool TryReadOwnershipFlag(
        ProcessMemory memory,
        ulong playerBaseAddress,
        InventoryOwnershipDefinition definition,
        out bool isOwned,
        out byte backingValue,
        out string error)
    {
        isOwned = false;
        backingValue = 0;

        if (!TryGetFlagLocation(definition, out var offset, out var mask, out error))
        {
            return false;
        }

        if (!TryReadByte(memory, playerBaseAddress, offset, definition.Name, out backingValue, out error))
        {
            return false;
        }

        isOwned = (backingValue & mask) != 0;
        return true;
    }

    public static bool TryWriteOwnershipFlag(
        ProcessMemory memory,
        ulong playerBaseAddress,
        InventoryOwnershipDefinition definition,
        bool isOwned,
        out byte oldBackingValue,
        out byte writtenBackingValue,
        out string error)
    {
        oldBackingValue = 0;
        writtenBackingValue = 0;

        if (!TryGetFlagLocation(definition, out var offset, out var mask, out error))
        {
            return false;
        }

        if (!TryReadByte(memory, playerBaseAddress, offset, definition.Name, out oldBackingValue, out error))
        {
            return false;
        }

        writtenBackingValue = isOwned
            ? (byte)(oldBackingValue | mask)
            : (byte)(oldBackingValue & ~mask);

        return TryWriteByte(memory, playerBaseAddress, offset, writtenBackingValue, definition.Name, out error);
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

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < InventoryDefinitions.SlotCount;
    }

    private static ulong GetSlotAddress(ulong playerBaseAddress, int slotIndex)
    {
        return playerBaseAddress + InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
    }

    private static bool TryGetFlagLocation(
        InventoryOwnershipDefinition definition,
        out uint offset,
        out byte mask,
        out string error)
    {
        offset = 0;
        mask = 0;
        error = string.Empty;

        if (!definition.FlagOffset.HasValue || !definition.FlagBit.HasValue)
        {
            error = $"{definition.Name} ownership flag is unknown.";
            return false;
        }

        if (definition.FlagBit.Value is < 0 or > 7)
        {
            error = $"{definition.Name} ownership bit {definition.FlagBit.Value} is outside the byte range.";
            return false;
        }

        offset = definition.FlagOffset.Value;
        mask = definition.Mask;
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
