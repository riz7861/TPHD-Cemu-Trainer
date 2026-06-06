namespace TphdCemuTrainer.Cheats;

public sealed record BombContentDefinition(byte Value, string Name)
{
    public override string ToString() => Name;
}
