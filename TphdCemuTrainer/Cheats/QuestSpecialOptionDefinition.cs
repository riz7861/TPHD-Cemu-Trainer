namespace TphdCemuTrainer.Cheats;

public sealed record QuestSpecialOptionDefinition(
    byte Value,
    string Name,
    string Notes)
{
    public override string ToString() => Name;
}
