namespace TphdCemuTrainer.Cheats;

public sealed record EquipmentOptionDefinition(byte Value, string Name)
{
    public string DisplayName => $"{Value} - {Name}";
}
