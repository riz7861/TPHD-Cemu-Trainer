namespace TphdCemuTrainer.Cheats;

public sealed record InventoryItemDefinition(byte ItemId, string Name, string Category)
{
    public string DisplayName => $"{ItemId} - {Name}";
}
