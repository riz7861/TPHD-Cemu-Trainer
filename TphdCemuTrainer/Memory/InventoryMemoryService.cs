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

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < InventoryDefinitions.SlotCount;
    }

    private static ulong GetSlotAddress(ulong playerBaseAddress, int slotIndex)
    {
        return playerBaseAddress + InventoryDefinitions.FirstSlotOffset + (uint)slotIndex;
    }
}
