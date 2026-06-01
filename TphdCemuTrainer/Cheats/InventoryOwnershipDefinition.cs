namespace TphdCemuTrainer.Cheats;

public sealed record InventoryOwnershipDefinition(
    string Id,
    string Name,
    IReadOnlyList<byte> DetectedItemIds,
    uint? FlagOffset,
    int? FlagBit,
    string Source,
    string Notes)
{
    public bool CanWrite => FlagOffset.HasValue && FlagBit.HasValue;

    public byte Mask => FlagBit.HasValue ? (byte)(1 << FlagBit.Value) : (byte)0;

    public string FlagLocation => FlagOffset.HasValue && FlagBit.HasValue
        ? $"0x{FlagOffset.Value:X} bit {FlagBit.Value}"
        : "Ownership/progression flag not mapped yet";

    public string EditStatus => CanWrite ? "Writable" : "Detection only";
}
