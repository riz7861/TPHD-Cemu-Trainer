namespace TphdCemuTrainer.Cheats;

public sealed record QuestSpecialItemDefinition(
    string Id,
    string Name,
    string SlotId,
    byte Value,
    IReadOnlyList<byte> DetectedValues);
