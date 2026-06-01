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
        BeforeBinary = FormatBinary(beforeValue);
        AfterBinary = FormatBinary(afterValue);
        ChangedBitMask = FormatChangedBitMask(beforeValue, afterValue);
        ChangedBits = FormatChangedBits(beforeValue, afterValue);
        ChangedBitCount = CountChangedBits(beforeValue, afterValue);
        BeforeBitCells = CreateBitCells(beforeValue, beforeValue, afterValue);
        AfterBitCells = CreateBitCells(afterValue, beforeValue, afterValue);
    }

    public uint OffsetValue { get; }

    public byte? BeforeValue { get; }

    public byte? AfterValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public string BeforeByte => FormatByte(BeforeValue);

    public string AfterByte => FormatByte(AfterValue);

    public bool IsChanged => BeforeValue != AfterValue;

    public string ByteChanged => IsChanged ? "Yes" : "No";

    public string BeforeBinary { get; }

    public string AfterBinary { get; }

    public string ChangedBitMask { get; }

    public string ChangedBits { get; }

    public int ChangedBitCount { get; }

    public IReadOnlyList<BitDisplayViewModel> BeforeBitCells { get; }

    public IReadOnlyList<BitDisplayViewModel> AfterBitCells { get; }

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

    private static string FormatBinary(byte? value)
    {
        return value.HasValue
            ? Convert.ToString(value.Value, 2).PadLeft(8, '0')
            : "--------";
    }

    private static string FormatChangedBitMask(byte? beforeValue, byte? afterValue)
    {
        if (!beforeValue.HasValue || !afterValue.HasValue)
        {
            return "--------";
        }

        var changedMask = beforeValue.Value ^ afterValue.Value;
        var characters = new char[8];
        for (var displayIndex = 0; displayIndex < characters.Length; displayIndex++)
        {
            var bitIndex = 7 - displayIndex;
            characters[displayIndex] = ((changedMask & (1 << bitIndex)) != 0) ? '^' : '.';
        }

        return new string(characters);
    }

    private static string FormatChangedBits(byte? beforeValue, byte? afterValue)
    {
        if (!beforeValue.HasValue || !afterValue.HasValue)
        {
            return "n/a";
        }

        var changedMask = beforeValue.Value ^ afterValue.Value;
        if (changedMask == 0)
        {
            return "-";
        }

        var bits = new List<string>();
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
            if ((changedMask & (1 << bitIndex)) != 0)
            {
                bits.Add($"bit {bitIndex}");
            }
        }

        return string.Join(", ", bits);
    }

    private static int CountChangedBits(byte? beforeValue, byte? afterValue)
    {
        if (!beforeValue.HasValue || !afterValue.HasValue)
        {
            return 0;
        }

        var changedMask = beforeValue.Value ^ afterValue.Value;
        var count = 0;
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
            if ((changedMask & (1 << bitIndex)) != 0)
            {
                count++;
            }
        }

        return count;
    }

    private static IReadOnlyList<BitDisplayViewModel> CreateBitCells(
        byte? displayedValue,
        byte? beforeValue,
        byte? afterValue)
    {
        var changedMask = beforeValue.HasValue && afterValue.HasValue
            ? beforeValue.Value ^ afterValue.Value
            : 0;
        var cells = new List<BitDisplayViewModel>(8);
        for (var bitIndex = 7; bitIndex >= 0; bitIndex--)
        {
            var value = displayedValue.HasValue
                ? ((displayedValue.Value & (1 << bitIndex)) != 0 ? '1' : '0')
                : '-';
            cells.Add(new BitDisplayViewModel(bitIndex, value, (changedMask & (1 << bitIndex)) != 0));
        }

        return cells;
    }
}
