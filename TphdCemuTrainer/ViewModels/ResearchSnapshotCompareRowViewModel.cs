namespace TphdCemuTrainer.ViewModels;

public sealed class ResearchSnapshotCompareRowViewModel
{
    public ResearchSnapshotCompareRowViewModel(uint offset, byte? snapshotAValue, byte? snapshotBValue)
    {
        OffsetValue = offset;
        SnapshotAValue = snapshotAValue;
        SnapshotBValue = snapshotBValue;
    }

    public uint OffsetValue { get; }

    public byte? SnapshotAValue { get; }

    public byte? SnapshotBValue { get; }

    public string Offset => $"0x{OffsetValue:X}";

    public string SnapshotAByte => FormatByte(SnapshotAValue);

    public string SnapshotBByte => FormatByte(SnapshotBValue);

    public string Difference => SnapshotAValue.HasValue && SnapshotBValue.HasValue
        ? ((int)SnapshotBValue.Value - SnapshotAValue.Value).ToString()
        : "n/a";

    public string SnapshotADecode => SnapshotAValue.HasValue
        ? Cheats.InventoryDefinitions.GetKnownItemName(SnapshotAValue.Value)
        : "-";

    public string SnapshotBDecode => SnapshotBValue.HasValue
        ? Cheats.InventoryDefinitions.GetKnownItemName(SnapshotBValue.Value)
        : "-";

    public bool IsChanged => SnapshotAValue != SnapshotBValue;

    private static string FormatByte(byte? value)
    {
        return value.HasValue ? $"{value.Value} / 0x{value.Value:X2}" : "n/a";
    }
}
