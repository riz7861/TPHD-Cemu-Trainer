namespace TphdCemuTrainer.Cheats;

public sealed record HiddenSkillDefinition(
    string Id,
    string Name,
    uint Offset,
    int Bit,
    string Evidence)
{
    public string OffsetText => $"0x{Offset:X}";
}
