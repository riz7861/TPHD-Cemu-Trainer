namespace TphdCemuTrainer.Cheats;

public sealed record GoldenBugBitDefinition(
    uint Offset,
    int Bit,
    string? ConfirmedBugName,
    string MappingStatus,
    string Notes)
{
    public string OffsetText => $"0x{Offset:X}";

    public bool IsConfirmed => !string.IsNullOrWhiteSpace(ConfirmedBugName);
}
