namespace TphdCemuTrainer.Cheats;

public sealed record InventoryOwnershipDefinition(
    string Id,
    string Name,
    IReadOnlyList<byte> DetectedItemIds,
    uint? FlagOffset,
    int? FlagBit,
    string Source,
    string Notes,
    uint? StaticSlotOffset = null,
    byte? StaticItemId = null,
    string FixedSlotStatus = "")
{
    public bool CanWrite => FlagOffset.HasValue && FlagBit.HasValue;

    public bool HasStaticVisibleSlotMapping => StaticSlotOffset.HasValue && StaticItemId.HasValue;

    public int? StaticSlotIndex => StaticSlotOffset.HasValue
        ? (int)(StaticSlotOffset.Value - InventoryDefinitions.FirstSlotOffset)
        : null;

    public byte Mask => FlagBit.HasValue ? (byte)(1 << FlagBit.Value) : (byte)0;

    public string FlagLocation => FlagOffset.HasValue && FlagBit.HasValue
        ? $"0x{FlagOffset.Value:X} bit {FlagBit.Value}"
        : "Ownership/progression flag not mapped yet";

    public string EditStatus => !string.IsNullOrWhiteSpace(FixedSlotStatus)
        ? FixedSlotStatus
        : CanWrite
            ? "Writable"
            : HasStaticVisibleSlotMapping
                ? "Fixed-slot mapped"
                : "Detection only";
}
