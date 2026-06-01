using System.Globalization;

namespace TphdCemuTrainer.Memory;

public enum OwnershipEditAcceptance
{
    Unknown,
    LikelyNo,
    LikelyYes
}

public sealed record ProgressionState(
    ulong PlayerBaseAddress,
    bool HasPlayerData,
    bool InventoryInitialized,
    bool EquipmentInitialized,
    OwnershipEditAcceptance GameAcceptsOwnershipEdits,
    string OwnershipEditDetectionReason,
    byte[] InventoryRawBytes,
    byte ArmorOwnershipByte,
    byte EquipmentOwnershipByte,
    byte MasterSwordInfusedByte,
    byte EquippedArmor,
    byte EquippedSword,
    byte EquippedShield)
{
    public string PlayerBaseText => HasPlayerData
        ? $"0x{PlayerBaseAddress:X}"
        : "-";

    public string PlayerDataStatus => HasPlayerData ? "Ready" : "Not Ready";

    public string InventoryStatus => InventoryInitialized ? "Ready" : "Not Initialized";

    public string EquipmentStatus => EquipmentInitialized ? "Ready" : "Not Initialized";

    public bool MemoryInitialized => InventoryInitialized && EquipmentInitialized;

    public string MemoryInitializedStatus => MemoryInitialized ? "Ready" : "Not Ready";

    public string OwnershipEditAcceptanceText => GameAcceptsOwnershipEdits switch
    {
        OwnershipEditAcceptance.LikelyYes => "Likely Yes",
        OwnershipEditAcceptance.LikelyNo => "Likely No",
        _ => "Unknown"
    };

    public string InventoryRawBytesText => InventoryRawBytes.Length == 0
        ? "Not read"
        : string.Join(" ", InventoryRawBytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));

    public string EquipmentOwnershipBytesText =>
        $"0x28D={ArmorOwnershipByte:X2} 0x28E={EquipmentOwnershipByte:X2} 0x292={MasterSwordInfusedByte:X2}";

    public string EquipmentEquippedBytesText =>
        $"armor=0x{EquippedArmor:X2} sword=0x{EquippedSword:X2} shield=0x{EquippedShield:X2}";
}
