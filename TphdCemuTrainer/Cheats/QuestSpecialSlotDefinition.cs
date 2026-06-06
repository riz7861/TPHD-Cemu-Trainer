namespace TphdCemuTrainer.Cheats;

public sealed record QuestSpecialSlotDefinition(
    string Id,
    string Name,
    uint Offset,
    string Notes,
    IReadOnlyList<QuestSpecialOptionDefinition> Options)
{
    public string OffsetText => $"0x{Offset:X}";
}
