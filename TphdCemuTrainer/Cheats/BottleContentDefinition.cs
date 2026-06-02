namespace TphdCemuTrainer.Cheats;

public sealed record BottleContentDefinition(byte ItemId, string Name, string Status)
{
    public string DisplayName => $"{Name} ({ItemId} / 0x{ItemId:X2})";
}
