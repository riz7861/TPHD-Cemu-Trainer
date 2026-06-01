namespace TphdCemuTrainer.Cheats;

public sealed record EquipmentFlagDefinition(
    string Id,
    string Name,
    uint Offset,
    int Bit,
    string Source)
{
    public byte Mask => (byte)(1 << Bit);

    public string OffsetText => $"0x{Offset:X}";
}
