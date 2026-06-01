namespace TphdCemuTrainer.Cheats;

public sealed record InventoryFixedSlotDefinition(
    string Id,
    string Name,
    int SlotIndex,
    IReadOnlyList<InventoryItemDefinition> AllowedItems,
    bool IsGameManaged,
    string Source,
    string Notes)
{
    public int SlotNumber => SlotIndex + 1;

    public uint OffsetValue => InventoryDefinitions.FirstSlotOffset + (uint)SlotIndex;

    public string Offset => $"0x{OffsetValue:X}";

    public string ManagementStatus => IsGameManaged ? "Game-managed" : "User-managed";
}
