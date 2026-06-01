using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class OwnershipDiscoveryRowViewModel
{
    public OwnershipDiscoveryRowViewModel(
        uint offset,
        byte? beforeValue,
        byte? afterValue,
        bool treatAfterAsPersisted,
        string itemContext)
    {
        OffsetValue = offset;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        IsOutsideVisibleInventorySlots = offset < InventoryDefinitions.FirstSlotOffset ||
            offset >= InventoryDefinitions.FirstSlotOffset + InventoryDefinitions.SlotCount;
        PersistedAfterReload = IsChanged && treatAfterAsPersisted;
        Score =
            (IsChanged ? 1 : 0) +
            (IsOutsideVisibleInventorySlots && IsChanged ? 1 : 0) +
            (PersistedAfterReload ? 1 : 0);
        IsStrongCandidate = IsChanged && IsOutsideVisibleInventorySlots && PersistedAfterReload;
        PotentialMeaning = GetPotentialMeaning(offset, beforeValue, afterValue, itemContext);
    }

    public uint OffsetValue { get; }

    public byte? BeforeValue { get; }

    public byte? AfterValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public string BeforeByte => FormatByte(BeforeValue);

    public string AfterByte => FormatByte(AfterValue);

    public bool IsChanged => BeforeValue != AfterValue;

    public bool PersistedAfterReload { get; }

    public bool IsOutsideVisibleInventorySlots { get; }

    public int Score { get; }

    public bool IsStrongCandidate { get; }

    public string PotentialMeaning { get; }

    private static string GetPotentialMeaning(uint offset, byte? beforeValue, byte? afterValue, string itemContext)
    {
        if (!beforeValue.HasValue || !afterValue.HasValue)
        {
            return "Missing from one snapshot";
        }

        if (offset >= InventoryDefinitions.FirstSlotOffset &&
            offset < InventoryDefinitions.FirstSlotOffset + InventoryDefinitions.SlotCount)
        {
            var decoded = InventoryDefinitions.GetKnownItemName(afterValue.Value);
            return decoded == "-"
                ? "Visible inventory slot"
                : $"Visible {decoded} slot";
        }

        var changedEquipmentFlags = EquipmentDefinitions.OwnershipFlags
            .Where(flag => flag.Offset == offset && (((beforeValue.Value ^ afterValue.Value) & flag.Mask) != 0))
            .Select(flag => flag.Name)
            .ToList();
        if (changedEquipmentFlags.Count > 0)
        {
            return $"Known equipment ownership byte: {string.Join(", ", changedEquipmentFlags)}";
        }

        if (offset == CheatCatalog.GetValue(CheatId.GoldenBugsFlags).Offset)
        {
            return string.IsNullOrWhiteSpace(itemContext)
                ? "CT Golden Bugs bitfield / collectible byte; investigate before treating as inventory ownership"
                : $"Potential {itemContext} ownership/progression flag candidate; CT also labels this byte as Golden Bugs/collectibles, verify carefully";
        }

        if (offset == CheatCatalog.GetValue(CheatId.PoeSouls).Offset)
        {
            return "Poe Souls collectible byte";
        }

        if (offset >= 0x2A1 && offset <= 0x2CC)
        {
            return string.IsNullOrWhiteSpace(itemContext)
                ? "Collectible/ammo candidate region; investigate persistence and item context"
                : $"Potential {itemContext} ownership/progression flag candidate in collectible/ammo region";
        }

        if (offset >= 0x240 && offset <= 0x2DF)
        {
            return string.IsNullOrWhiteSpace(itemContext)
                ? "Potential ownership/progression flag candidate"
                : $"Potential {itemContext} ownership/progression flag candidate";
        }

        return "Changed byte outside visible inventory slots";
    }

    private static string FormatByte(byte? value)
    {
        return value.HasValue ? $"{value.Value} / 0x{value.Value:X2}" : "n/a";
    }
}
