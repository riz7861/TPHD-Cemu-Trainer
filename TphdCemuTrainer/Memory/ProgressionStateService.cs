using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.Memory;

public static class ProgressionStateService
{
    public const uint ArmorOwnershipOffset = 0x28D;
    public const uint EquipmentOwnershipOffset = 0x28E;
    public const uint MasterSwordInfusedOffset = 0x292;

    private const byte ArmorOwnershipInitializedMask = 0x03;
    private const byte EquipmentOwnershipInitializedMask = 0x9F;
    private const byte MasterSwordInfusedMask = 0x02;
    private const byte EquippedArmorNoneOrDefault = 46;
    private const byte EquippedSwordNone = 255;
    private const byte EquippedShieldNone = 255;

    public static bool TryReadState(
        ProcessMemory memory,
        ulong playerBaseAddress,
        out ProgressionState state,
        out string error)
    {
        error = string.Empty;
        state = CreateUnavailable();

        var inventoryAddress = playerBaseAddress + InventoryDefinitions.FirstSlotOffset;
        if (!memory.TryReadBytes(
                inventoryAddress,
                InventoryDefinitions.SlotCount,
                out var inventoryRawBytes,
                out var inventoryBytesRead) ||
            inventoryBytesRead != InventoryDefinitions.SlotCount)
        {
            error = $"Could not read inventory initialization bytes at 0x{inventoryAddress:X}.";
            return false;
        }

        if (!TryReadByte(memory, playerBaseAddress, ArmorOwnershipOffset, out var armorOwnershipByte, out error) ||
            !TryReadByte(memory, playerBaseAddress, EquipmentOwnershipOffset, out var equipmentOwnershipByte, out error) ||
            !TryReadByte(memory, playerBaseAddress, MasterSwordInfusedOffset, out var masterSwordInfusedByte, out error) ||
            !TryReadByte(memory, playerBaseAddress, EquipmentDefinitions.ArmorSlot.Offset, out var equippedArmor, out error) ||
            !TryReadByte(memory, playerBaseAddress, EquipmentDefinitions.SwordSlot.Offset, out var equippedSword, out error) ||
            !TryReadByte(memory, playerBaseAddress, EquipmentDefinitions.ShieldSlot.Offset, out var equippedShield, out error))
        {
            return false;
        }

        var inventoryInitialized = IsInventoryInitialized(inventoryRawBytes);
        var equipmentInitialized = IsEquipmentInitialized(
            armorOwnershipByte,
            equipmentOwnershipByte,
            masterSwordInfusedByte,
            equippedArmor,
            equippedSword,
            equippedShield);
        var ownershipAcceptance = EvaluateOwnershipEditAcceptance(
            HasPlayerData: true,
            inventoryInitialized,
            equipmentInitialized);

        state = new ProgressionState(
            playerBaseAddress,
            HasPlayerData: true,
            InventoryInitialized: inventoryInitialized,
            EquipmentInitialized: equipmentInitialized,
            GameAcceptsOwnershipEdits: ownershipAcceptance.Acceptance,
            OwnershipEditDetectionReason: ownershipAcceptance.Reason,
            InventoryRawBytes: inventoryRawBytes,
            ArmorOwnershipByte: armorOwnershipByte,
            EquipmentOwnershipByte: equipmentOwnershipByte,
            MasterSwordInfusedByte: masterSwordInfusedByte,
            EquippedArmor: equippedArmor,
            EquippedSword: equippedSword,
            EquippedShield: equippedShield);

        return true;
    }

    public static ProgressionState CreateUnavailable()
    {
        return new ProgressionState(
            0,
            HasPlayerData: false,
            InventoryInitialized: false,
            EquipmentInitialized: false,
            GameAcceptsOwnershipEdits: OwnershipEditAcceptance.Unknown,
            OwnershipEditDetectionReason: "Player data is not available.",
            InventoryRawBytes: [],
            ArmorOwnershipByte: 0,
            EquipmentOwnershipByte: 0,
            MasterSwordInfusedByte: 0,
            EquippedArmor: 0,
            EquippedSword: 0,
            EquippedShield: 0);
    }

    public static bool IsInventoryInitialized(IReadOnlyCollection<byte> inventoryRawBytes)
    {
        return inventoryRawBytes.Any(value => value != InventoryDefinitions.EmptyItemId);
    }

    public static bool IsEquipmentInitialized(
        byte armorOwnershipByte,
        byte equipmentOwnershipByte,
        byte masterSwordInfusedByte,
        byte equippedArmor,
        byte equippedSword,
        byte equippedShield)
    {
        var allOwnershipFlagsFalse =
            (armorOwnershipByte & ArmorOwnershipInitializedMask) == 0 &&
            (equipmentOwnershipByte & EquipmentOwnershipInitializedMask) == 0 &&
            (masterSwordInfusedByte & MasterSwordInfusedMask) == 0;

        var allEquippedDefaults =
            equippedArmor == EquippedArmorNoneOrDefault &&
            equippedSword == EquippedSwordNone &&
            equippedShield == EquippedShieldNone;

        return !(allOwnershipFlagsFalse && allEquippedDefaults);
    }

    public static (OwnershipEditAcceptance Acceptance, string Reason) EvaluateOwnershipEditAcceptance(
        bool HasPlayerData,
        bool inventoryInitialized,
        bool equipmentInitialized)
    {
        if (!HasPlayerData)
        {
            return (OwnershipEditAcceptance.Unknown, "Player data is not available.");
        }

        if (inventoryInitialized && equipmentInitialized)
        {
            return (
                OwnershipEditAcceptance.LikelyYes,
                "Heuristic: inventory and equipment memory are initialized, which is consistent with a save past the Ordon Village intro arc.");
        }

        return (
            OwnershipEditAcceptance.LikelyNo,
            "Heuristic: player data is present, but inventory and equipment memory are not both initialized. Early Ordon intro saves may accept writes in memory while TPHD ignores ownership edits.");
    }

    private static bool TryReadByte(
        ProcessMemory memory,
        ulong playerBaseAddress,
        uint offset,
        out byte value,
        out string error)
    {
        value = 0;
        var address = playerBaseAddress + offset;
        if (!memory.TryReadBytes(address, 1, out var bytes, out var bytesRead) || bytesRead != 1)
        {
            error = $"Could not read progression byte at _playerbase+0x{offset:X} / 0x{address:X}.";
            return false;
        }

        value = bytes[0];
        error = string.Empty;
        return true;
    }
}
