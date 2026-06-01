namespace TphdCemuTrainer.Cheats;

public sealed record InventoryOwnershipDefinition(
    string Id,
    string Name,
    IReadOnlyList<byte> DetectedItemIds,
    uint? FlagOffset,
    int? FlagBit,
    bool CanWrite,
    string Notes)
{
    public string FlagLocation => FlagOffset.HasValue && FlagBit.HasValue
        ? $"0x{FlagOffset.Value:X} bit {FlagBit.Value}"
        : "Ownership flag unknown";

    public string EditStatus => CanWrite ? "Writable" : "Not implemented";
}
