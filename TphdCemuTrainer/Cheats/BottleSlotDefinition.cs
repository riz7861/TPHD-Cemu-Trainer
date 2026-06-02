namespace TphdCemuTrainer.Cheats;

public sealed record BottleSlotDefinition(int BottleSlotNumber, uint Offset)
{
    public int InventorySlotIndex => (int)(Offset - InventoryDefinitions.FirstSlotOffset);

    public string OffsetText => $"0x{Offset:X}";
}
