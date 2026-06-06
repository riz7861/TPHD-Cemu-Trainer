namespace TphdCemuTrainer.Cheats;

public sealed record HeartProgressOption(int PartialPieces, string Label)
{
    public override string ToString() => Label;
}
