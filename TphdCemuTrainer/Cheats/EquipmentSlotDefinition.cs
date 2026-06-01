namespace TphdCemuTrainer.Cheats;

public sealed record EquipmentSlotDefinition(
    string Id,
    string Name,
    uint Offset,
    IReadOnlyList<EquipmentOptionDefinition> Options,
    string Source)
{
    public string OffsetText => $"0x{Offset:X}";
}
