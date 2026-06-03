namespace TphdCemuTrainer.Cheats;

public sealed record BombSlotDefinition(int SlotNumber, uint Offset)
{
    public string OffsetText => $"0x{Offset:X}";
}
