namespace TphdCemuTrainer.Cheats;

public sealed record QuestBitFlagDefinition(
    string Id,
    string Name,
    uint Offset,
    int Bit,
    string Notes)
{
    public string OffsetText => $"0x{Offset:X}";

    public string BitText => $"bit {Bit}";
}
