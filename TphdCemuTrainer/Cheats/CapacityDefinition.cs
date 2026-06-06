namespace TphdCemuTrainer.Cheats;

public sealed record CapacityOption(string Label, int Capacity, int StoredValue)
{
    public override string ToString() => Label;
}

public sealed record CapacityDefinition(
    string Id,
    string Name,
    uint? Offset,
    CheatValueKind? ValueKind,
    IReadOnlyList<CapacityOption> Options,
    string Source,
    int DefaultCapacity)
{
    public bool IsMemoryBacked => Offset.HasValue && ValueKind.HasValue;
}
