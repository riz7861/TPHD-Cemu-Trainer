namespace TphdCemuTrainer.Cheats;

public sealed record StampBitDefinition(uint Offset, int Bit, string Name, string Notes)
{
    public string OffsetText => $"0x{Offset:X}";
}
